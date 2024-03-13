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
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = "D:\\Dev\\db_update\\",
                Filter = "xml files (*.xml)|*.xml|csv files (*.csv)|*.csv|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                sourceFiles = openFileDialog.FileNames;
            }
            listBox2.Items.Clear();


            foreach (string file in sourceFiles)
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
                foreach (DataTable table in ds.Tables)
                {

                    columnNames.AddRange(table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());

                }

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


            using (var connection = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
            {
                connection.Open();

                foreach (DataTable table in ds.Tables)
                {
                    var insertCommand = new SQLiteCommand($"INSERT INTO {tableName} ({string.Join(", ", listBox3.Items)}) VALUES ({string.Join(", ", listBox3.Items.Cast<DataColumn>().Select(c => $"@p{c.Ordinal}"))})", connection);

                    for (int i = 0; i < table.Columns.Count; i++)
                    {if (listBox2.Items.Contains(table.Columns[i].ColumnName))
                        {
                            insertCommand.Parameters.Add($"p{i}", DbType.String);
                        }
                    else { table.Columns.Remove(table.Columns[i].ColumnName); }
                        
                    }

                    foreach (DataRow row in table.Rows)
                    {
                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            insertCommand.Parameters[$"p{i}"].Value = row[i]?.ToString();
                        }

                        insertCommand.ExecuteNonQuery();
                    }
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

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            tableName = listBox1.SelectedItem.ToString();
            listBox3.Items.AddRange(SQLiteRequestToList(dbFile, $"PRAGMA table_info('{tableName}');", 1).ToArray());
        }

        private void listBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.U && listBox2.SelectedIndex > 0)
            {
                string selectedItem = listBox2.SelectedItem.ToString();

                int selectedIndex = listBox2.SelectedIndex;
                listBox2.Items.Insert(selectedIndex - 1, selectedItem);

                listBox2.SelectedIndex = selectedIndex - 1;

                listBox2.Items.RemoveAt(selectedIndex + 1);
            }
            else if (e.KeyCode == Keys.Delete)
            { listBox2.Items.RemoveAt(listBox2.SelectedIndex);}
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
            { listBox2.Items.RemoveAt(listBox2.SelectedIndex); }
        }
    }
}
