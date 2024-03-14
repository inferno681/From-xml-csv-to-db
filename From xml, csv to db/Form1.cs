using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Data.SqlClient;
using static System.Net.WebRequestMethods;
using System.Diagnostics;
using System.Data.SQLite;
using System.Reflection.Emit;

namespace From_xml__csv_to_db
{
    public partial class Form1 : Form
    {
        string[] sourceFiles;
        string dbFile;
        string tableName;
        List<string> columnNames = new List<string>();
        List<string> tableNames = new List<string>();
        DataSet ds = new DataSet();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<string> sourceFilesShort = new List<string>();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = "D:\\Dev\\db_update\\",
                Filter = "xml files (*.xml)|*.xml|csv files (*.csv)|*.csv|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true,
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                sourceFiles = openFileDialog.FileNames;
            }
            listBox2.Items.Clear();
            

            if (sourceFiles.Count() >5)
            { 
                for (int i = 0; i < 5; i++)
                { sourceFilesShort.Add(sourceFiles[i]); }
            }
            else { sourceFilesShort.AddRange(sourceFiles); }

            ds = collectedDataCSV_XML(sourceFilesShort.ToArray(), true, progressBar1);
            foreach (DataTable table in ds.Tables)
                {

                    columnNames.AddRange(table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());

                }

            
            listBox2.Items.AddRange(columnNames.Distinct().ToArray());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = "D:\\Dev\\db_update\\",
                Filter = "sqlite files (*.db)|*.db|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                listBox1.Items.Clear();
                dbFile = openFileDialog.FileName;
            }
            tableNames = SQLiteRequestToList(dbFile, "SELECT name FROM sqlite_master WHERE type = 'table';");
            listBox1.Items.AddRange(tableNames.ToArray());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (sourceFiles == null || sourceFiles.Length == 0)
            {
                MessageBox.Show("Please select one or more XML or CSV files first.");
                return;
            }
            if (dbFile == null)
            {
                MessageBox.Show("Please select db file first.");
                return;
            }
            List<string> sourceFilesShort = new List<string>();
            if (sourceFiles.Count() <= 5)
            {
                ds = collectedDataCSV_XML(sourceFiles, true, progressBar2);
                SQLiteInsert(dbFile, ds, tableName, listBox2, listBox3, true, progressBar3);
            }
            else 
            {
                for (int i = 0; i < sourceFiles.Count(); i++)
                {
                    sourceFilesShort.Add(sourceFiles[i]);
                    if (i % 5 == 0)
                    {
                        ds = collectedDataCSV_XML(sourceFilesShort.ToArray(), true, progressBar2);
                        SQLiteInsert(dbFile, ds, tableName, listBox2, listBox3, true, progressBar3);
                        sourceFilesShort.Clear();
                    }
                    else if (i > sourceFiles.Count() / 5 )

                }
             }
            
        }
        static List<string> SQLiteRequestToList(string dataBase, string SQLRequest, int row = 0)
        {
            List<string> SQLiteList = new List<string>();
            using (var connection = new SQLiteConnection($"Data Source={dataBase};Version=3;"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(SQLRequest, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SQLiteList.Add(reader.GetString(row));
                        }

                    }

                }
            }
            return SQLiteList;
        }
        static DataSet collectedDataCSV_XML(string[] source, bool isProgressBar = false, ProgressBar progressBar = null)
        {
            if (isProgressBar)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = source.Count();
            }

            DataSet ds = new DataSet();
            foreach (string file in source)
            {
                if (Path.GetExtension(file).ToLower() == ".xml")
                {
                    ds.ReadXml(file);
                }
                else if (Path.GetExtension(file).ToLower() == ".csv")
                {
                    DataTable table = new DataTable();
                    using (StreamReader reader = new StreamReader(file))
                    {
                        string[] headers = reader.ReadLine().Split(',');
                        foreach (string header in headers)
                        {
                            table.Columns.Add(header);
                        }

                        while (!reader.EndOfStream)
                        {
                            string[] values = reader.ReadLine().Split(',');
                            DataRow row = table.NewRow();
                            for (int i = 0; i < values.Length; i++)
                            {
                                row[i] = values[i];
                            }
                            table.Rows.Add(row);
                        }
                    }
                    ds.Tables.Add(table);
                }
            }
            if (isProgressBar)
            {
                progressBar.PerformStep();
            }
            return ds;
        }
        static void SQLiteInsert(string db, DataSet source, string tableName, ListBox listBoxDS, ListBox listBoxDB, bool isProgressBar = false, ProgressBar progressBar = null)
        {
            if (isProgressBar)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = source.Tables.Count;
            }
            using (var connection = new SQLiteConnection($"Data Source={db};Version=3;"))
            {
                connection.Open();

                foreach (DataTable table in source.Tables)
                {
                    string valInsert = "";
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        if (listBoxDS.Items.Contains(table.Columns[i].ColumnName))
                        {
                            continue;
                        }
                        table.Columns.RemoveAt(i);
                    }

                    foreach (DataRow row in table.Rows)
                    {

                        valInsert += "('" + string.Join("', '", row.ItemArray) + "'), ";


                    }
                    var insertCommand = new SQLiteCommand($"INSERT INTO {tableName} ({string.Join(", ", listBoxDB.Items.Cast<string>())}) VALUES {valInsert.Remove(valInsert.Length - 2).Replace("NaN", null)}", connection);
                    insertCommand.ExecuteNonQuery();
                    if (isProgressBar)
                    {
                        progressBar.PerformStep();
                    }
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            tableName = listBox1.SelectedItem.ToString();
            listBox3.Items.AddRange(SQLiteRequestToList(dbFile, $"PRAGMA table_info('{tableName}');", 1).ToArray());
        }


        private void listBox3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.U && listBox3.SelectedIndex > 0)
            {

                string selectedItem = listBox3.SelectedItem.ToString();

                int selectedIndex = listBox3.SelectedIndex;
                listBox3.Items.Insert(selectedIndex - 1, selectedItem);

                listBox3.SelectedIndex = selectedIndex - 1;

                listBox3.Items.RemoveAt(selectedIndex + 1);
            }
            else if (e.KeyCode == Keys.Delete)
            { listBox3.Items.RemoveAt(listBox3.SelectedIndex); }
        }
    }
}
