﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UBalance.Library.AppLoading;
using UBalance.Library.Classes;
using UBalance.Library.Events;
using UBalance.Properties;

namespace UBalance
{
    public partial class UBalance : Form
    {
        public static AppLoader App = new AppLoader();
        public static FolderBrowserDialog FolderBroswer = new FolderBrowserDialog();
        public GridData ViewData;
        public BalanceReader Balance;
        public ColorReader Color;
        public AppParser AppParse;

        public string BalancePort = String.Empty;
        public string ColorPort = String.Empty;

        public SerialType SingleSerialType;

        public UBalance()
        {
            InitializeComponent();
            Balance = DefaultBalance();

            if (!Balance.IsBalanceConnected())
            {
                Balance = null;
                readBalanceButton.Enabled = false;
                readBalanceButton.Text = @"Disconnected";
            }
            else
            {
                // update command being sent to the balance on click
                Balance.UpdateCommand();
            }

            addRowButton.Enabled = false;
        }

        private BalanceReader DefaultBalance()
        {
            // Get default settings and create balance reader off of those options
            string comPort = BalancePort == String.Empty ? Settings.Default.COMPort : BalancePort;          //1 - position in constructor
            int baudRate = Settings.Default.BaudRate;           //2
            int dataBits = Settings.Default.DataBits;           //4
            StopBits stopBits;                                  //3
            Parity parity;                                      //5
            string sicsCommand = Settings.Default.SICSCommand;  //6
            bool rts = Settings.Default.RTS;                    //7

            //Send sicsCommand to scale to update the value

            Enum.TryParse(Settings.Default.StopBits, out stopBits);
            Enum.TryParse(Settings.Default.Parity, out parity);

            return new BalanceReader(comPort, baudRate, stopBits, dataBits, parity, sicsCommand, rts);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int numConnectedPorts = SerialPort.GetPortNames().ToList().Count;

            if (numConnectedPorts > 1)
            {
                MultipleSerialPopup m = new MultipleSerialPopup();
                
                m.Closing += m_Closing;

                m.Show();
            }
            else if (numConnectedPorts == 1)
            {
                SingleSerialPopup s = new SingleSerialPopup();

                s.Closing += s_Closing;
                s.Show();
            }
            

            LoadAndUpdateAppFiles();
        }

        void s_Closing(object sender, CancelEventArgs e)
        {
            SingleSerialPopup singleSerialPopup = sender as SingleSerialPopup;
            if (singleSerialPopup == null) return;
            SingleSerialType = singleSerialPopup.PortType;

            if (SingleSerialType == SerialType.Balance)
            {
                BalancePort = singleSerialPopup.PortName;
            }
            else if (SingleSerialType == SerialType.ColorMeter)
            {
                ColorPort = singleSerialPopup.PortName;
            }
        }

        private void m_Closing(object sender, CancelEventArgs e)
        {
            MultipleSerialPopup multipleSerialPopup = sender as MultipleSerialPopup;

            if (multipleSerialPopup == null) return;

            if (multipleSerialPopup.BalancePortName != "Unused")
            {
                BalancePort = multipleSerialPopup.BalancePortName;
            }
            if (multipleSerialPopup.ColorPortName != "Unused")
            {
                ColorPort = multipleSerialPopup.ColorPortName;
            }
        }

        public void LoadAndUpdateAppFiles()
        {
            // remove the old toolstrip menu items if they exist
            if (appsToolStripMenuItem.DropDownItems.Count > 0)
            {
                while (appsToolStripMenuItem.DropDownItems.Count != 0)
                {
                    appsToolStripMenuItem.DropDownItems.RemoveAt(0);
                }
            }

            List<string> itemsToAddToMenu = App.GetFileNames();

            if (itemsToAddToMenu.Count == 0)
            {
                // no .APP files found in the directory, adding useful info for the user
                ToolStripMenuItem item = new ToolStripMenuItem();
                item.Enabled = false;
                item.Text = "No app items found";
                this.appsToolStripMenuItem.DropDownItems.Insert(0, item);
            }
            else
            {
                int i = 0;
                foreach (string s in itemsToAddToMenu)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem();
                    item.Enabled = true;
                    item.Text = Path.GetFileNameWithoutExtension(s);
                    item.Click += item_Click;
                    this.appsToolStripMenuItem.DropDownItems.Insert(i, item);
                    i++;
                }
            }
        }

        void item_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem t = sender as ToolStripMenuItem;

            //find item in app
            string nameOfApp = t.Text;

            EmptyGridView(nameOfApp);

            addRowButton.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            //textBox1.Text = app.GetDirectoryNames()[index];
            //.ShowDialog();
        }


        private string oldCellValue;
        void UBalanceDataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridViewCell d = UBalanceDataGridView[e.ColumnIndex, e.RowIndex];
            oldCellValue = d.Value == null ? null : d.Value.ToString();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Preferences pref = new Preferences();

            pref.CloseEvent += pref_CloseEvent;

            pref.Show();
        }

        private void pref_CloseEvent(object sender, PreferenceCloseEventArgs e)
        {
            App.UpdatePath(e.Text);
            // update balance settings
            if(Balance != null)
                Balance.Dispose();
            Balance = DefaultBalance();
            
            LoadAndUpdateAppFiles();

            readBalanceButton.Enabled = Balance.IsBalanceConnected();
        }

        private void addRowButton_Click(object sender, EventArgs e)
        {
            AddRowToDataAndView();
        }

        private void readBalanceButton_Click(object sender, EventArgs e)
        {
            DataGridViewCell cell = null;
            try
            {
                cell = UBalanceDataGridView.SelectedCells[0];
            }
            catch (Exception ex)
            {
                //ignore, keep cell null
            }
                

            if (cell != null)
            {
                cell.Value = Balance.GetBalanceValue();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog s = new SaveFileDialog();

            if (s.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(s.FileName, FileMode.Create);
                StreamWriter w = new StreamWriter(fs);

                w.Write(ViewData.Save());
                
                w.Close();
                fs.Close();
            }
        }
        private void EmptyGridView(string nameOfApp)
        {
            int index = App.GetFileNames().IndexOf(nameOfApp);

            if (index < 0) return;
            AppParse = new AppParser(App.GetDirectoryNames()[index]);
            ViewData = new GridData(AppParse.ReturnCellsInRow(UBalanceDataGridView.RowCount), nameOfApp);

            List<string> columnHeaders = ViewData.ColumnHeaders();

            int count = columnHeaders.Count;

            UBalanceDataGridView.ColumnCount = count;
            UBalanceDataGridView.CellBeginEdit += UBalanceDataGridView_CellBeginEdit;
            UBalanceDataGridView.CellEndEdit += UBalanceDataGridView_CellEndEdit;

            if (UBalanceDataGridView.RowCount > 0)
            {
                for (int i = UBalanceDataGridView.RowCount; i > 0; i--)// UBalanceDataGridView row in UBalanceDataGridView.Rows)
                {
                    UBalanceDataGridView.Rows.Remove(UBalanceDataGridView.Rows[i - 1]);
                }
            }

            UBalanceDataGridView.Rows.Add();

            for (int i = 0; i < count; i++)
            {
                UBalanceDataGridView.Columns[i].HeaderText = columnHeaders[i];
            }

            for (int i = 0; i < UBalanceDataGridView.Rows[0].Cells.Count; i++)
            {
                ViewData.GetCell(0, i).CellValueChanged += UBalance_CellValueChanged; 
                ViewData.GetCell(0, i).NotifyDependents += UBalance_NotifyDependents;

                UBalanceDataGridView.Columns[i].Resizable = DataGridViewTriState.True;
            }
        }
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog o = new OpenFileDialog();

            if (o.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(o.FileName, FileMode.Open);
                StreamReader r = new StreamReader(fs);

                // first line is the app name
                string appName = r.ReadLine();

                EmptyGridView(appName);
                
                string s;
                int row = 0, column;
                double placeHolder;
                bool firstLine = true;
                while ((s = r.ReadLine()) != null)
                {
                    if (!firstLine) 
                        AddRowToDataAndView();
                    var items = s.Split(',');
                    column = 0;

                    foreach (string item in items)
                    {
                        if (!String.IsNullOrWhiteSpace(item))
                        {
                            if (double.TryParse(item, out placeHolder))
                                ViewData.GetCell(row, column).Value = placeHolder;
                            // string, need to set correct string value for celltype
                            // only K has a string value
                            else
                            {
                                KCell kc = ViewData.GetCell(row, column) as KCell;
                                if (kc != null)
                                {
                                    kc.KValue = item;
                                }
                            }
                        }
                        column++;
                    }
                    firstLine = false;
                    row++;
                }

                r.Close();
                fs.Close();
                saveToolStripMenuItem.Enabled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.F1) && saveToolStripMenuItem.Enabled)
            {
                saveToolStripMenuItem_Click(this, EventArgs.Empty);
            }
            if (keyData == (Keys.F2))
            {
                loadToolStripMenuItem_Click(this, EventArgs.Empty);
            }
            if (keyData == (Keys.F3) && addRowButton.Enabled )
            {
                addRowButton_Click(this, EventArgs.Empty);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
