using dCom.Configuration;
using dCom.Connection;
using dCom.Modbus;
using dCom.Modbus.FunctionParameters;
using dCom.Modbus.ModbusFunctions;
using dCom.ViewModel;
using System;
using System.Threading;

namespace dCom.Acquisition
{
	public class Acquisitor : IDisposable
	{
		private AutoResetEvent acquisitionTrigger;
		private FunctionExecutor commandExecutor;
		private Thread acquisitionWorker;
		private IStateUpdater stateUpdater;
		private bool acquisitionStopSignal = true;
        private int DO_REG_sekunde = 0;
        private int HR_INT_sekunde = 0;

        public Acquisitor(AutoResetEvent acquisitionTrigger, FunctionExecutor commandExecutor, IStateUpdater stateUpdater)
		{
			this.stateUpdater = stateUpdater;
			this.acquisitionTrigger = acquisitionTrigger;
			this.commandExecutor = commandExecutor;
			this.InitializeAcquisitionThread();
			this.StartAcquisitionThread();
		}

		#region Private Methods

		private void InitializeAcquisitionThread()
		{
			this.acquisitionWorker = new Thread(Acquisition_DoWork);
			this.acquisitionWorker.Name = "Acquisition thread";
		}

		private void StartAcquisitionThread()
		{
			acquisitionWorker.Start();
		}

		/// <summary>
		/// Acquisition thread
		///		Awaits for trigger;
		///		After configured period send appropriate command to MdbSim for each point type
		/// </summary>
		private void Acquisition_DoWork()
		{
            while (true)
            {
                try
                {
                    acquisitionTrigger.WaitOne();
                    DO_REG_sekunde++;
                    
                    HR_INT_sekunde++;
                    if (DO_REG_sekunde == ConfigReader.Instance.GetAcquisitionInterval("DigOut"))
                    {
                        ModbusReadCommandParameters p = new ModbusReadCommandParameters(6, (byte)ModbusFunctionCode.READ_COILS, ConfigReader.Instance.GetStartAddress("DigOut"), ConfigReader.Instance.GetNumberOfRegisters("DigOut")); ;
                        ModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
                        this.commandExecutor.EnqueueCommand(fn);
                        DO_REG_sekunde = 0;
                    }
                    
                    if (HR_INT_sekunde == ConfigReader.Instance.GetAcquisitionInterval("AnaOut"))
                    {
                        ModbusReadCommandParameters p = new ModbusReadCommandParameters(6, (byte)ModbusFunctionCode.READ_HOLDING_REGISTERS, ConfigReader.Instance.GetStartAddress("AnaOut"), ConfigReader.Instance.GetNumberOfRegisters("AnaOut")); ;
                        ModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
                        this.commandExecutor.EnqueueCommand(fn);
                        HR_INT_sekunde = 0;
                    }

                }
                catch (Exception ex)
                {
                    string message = $"{ex.TargetSite.ReflectedType.Name}.{ex.TargetSite.Name}: {ex.Message}";
                    stateUpdater.LogMessage(message);
                }
            }
        }

		#endregion Private Methods

		public void Dispose()
		{
			acquisitionStopSignal = false;
		}
	}
}