using dCom.Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace dCom.Modbus.ModbusFunctions
{
	public class ReadInputRegistersFunction : ModbusFunction
	{
		public ReadInputRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
		{
			CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
		}

		/// <inheritdoc />
		public override byte[] PackRequest()
		{
            ModbusReadCommandParameters mdmReadCommParams = this.CommandParameters as ModbusReadCommandParameters;
            byte[] mdbRequest = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)mdmReadCommParams.TransactionId)), 0, mdbRequest, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)mdmReadCommParams.ProtocolId)), 0, mdbRequest, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)mdmReadCommParams.Length)), 0, mdbRequest, 4, 2);
            mdbRequest[6] = mdmReadCommParams.UnitId;
            mdbRequest[7] = mdmReadCommParams.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)mdmReadCommParams.StartAddress)), 0, mdbRequest, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)mdmReadCommParams.Quantity)), 0, mdbRequest, 10, 2);
            return mdbRequest;
        }

		/// <inheritdoc />
		public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
		{
            ModbusReadCommandParameters mdmReadCommParams = this.CommandParameters as ModbusReadCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> dic = new Dictionary<Tuple<PointType, ushort>, ushort>();
            int kolicina = response[8];
            int prosao = 0;
            ushort value = 0;
            int broo = 0;


            for (int i = 0; i < kolicina; i++)
            {
                ushort value1 = (ushort)(response[9 + i]);
                for (int j = 0; j < 8; j++)
                {
                    if (prosao != mdmReadCommParams.Quantity)
                    {
                        byte[] r = new byte[2];

                        r[1] = response[9 + broo];
                        r[0] = response[10 + broo];

                        value = (ushort)BitConverter.ToInt16(r, 0);

                        dic.Add(new Tuple<PointType, ushort>(PointType.ANALOG_INPUT, (ushort)(mdmReadCommParams.StartAddress + prosao)), value);

                        prosao++;
                        broo += 2;
                    }
                    else break;
                }
            }

            return dic;
        }
	}
}