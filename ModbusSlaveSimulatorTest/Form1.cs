using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using NModbus;

namespace ModbusSlaveSimulatorTest
{
    public partial class Form1 : Form
    {
        class AppData
        {
            public string BindIp = "0.0.0.0";
            public ushort BindPort = 502;
            public byte SlaveCount = 9;
            public bool SimulationEnable = true;
            public int SimulationFreq = 1000;
            public string SimulationUnitId = "1,2,3,4,5,6,7,8,9";
            public ushort SimulationRegisterMax = 1000;
            public ushort SimulationStep = 1;
        }

        TcpListener modbusListener;
        List<IModbusSlave> listSlave = new List<IModbusSlave>();
        IModbusSlaveNetwork modbusSlaveNetwork = null;
        Dictionary<ushort, bool> dictSlaves = new Dictionary<ushort, bool>();
        //数据仿真
        ushort uValue = 0;
        ushort uStep = 1;
        AppData appData = new AppData();
        IModbusFactory factory = new ModbusFactory();

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //创建通讯绑定端口
            modbusListener = new TcpListener(IPAddress.Parse(appData.BindIp), appData.BindPort);
            modbusListener.Start();

            modbusSlaveNetwork = factory.CreateSlaveNetwork(modbusListener);

            //创建Slave设备
            for (byte i = 0; i < appData.SlaveCount; i++)
            {
                IModbusSlave slave = factory.CreateSlave((byte)(i + 1));
                listSlave.Add(slave);
                modbusSlaveNetwork.AddSlave(slave);
                dictSlaves[(ushort)(i + 1)] = true;
            }

            //接受连接
            modbusSlaveNetwork.ListenAsync();

            comboBox1.SelectedIndex = 4;
            
        }

        private void checkBox9_MouseClick(object sender, MouseEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            byte slaveId = byte.Parse(cb.Text.Replace("Slave", ""));
            if (cb.Checked)
            {
                modbusSlaveNetwork.AddSlave(listSlave[slaveId - 1]);
                dictSlaves[slaveId] = true;
            }
            else
            {
                modbusSlaveNetwork.RemoveSlave(slaveId);
                dictSlaves.Remove(slaveId);
            }
        }



        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox10.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bool[] vb = new bool[appData.SimulationRegisterMax];
            ushort[] vu = new ushort[appData.SimulationRegisterMax];
            for (int i = 0; i < appData.SimulationRegisterMax; i++)
            {
                vu[i] = uValue;
                vb[i] = (uValue % 2 == 1) ? true : false;
            }

            foreach (var slave in listSlave)
            {
                if (!dictSlaves.ContainsKey(slave.UnitId))
                    continue;
                slave.DataStore.CoilDiscretes.WritePoints(0, vb);
                slave.DataStore.CoilInputs.WritePoints(0, vb);
                slave.DataStore.InputRegisters.WritePoints(0, vu);
                slave.DataStore.HoldingRegisters.WritePoints(0, vu);
            }

            uValue += uStep;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            timer1.Interval = int.Parse(comboBox1.Text);
        }
    }
}
