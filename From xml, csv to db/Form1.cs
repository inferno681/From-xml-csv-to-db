﻿using System;
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
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "D:\\Dev\\db_update\\";
            openFileDialog.Filter = "xml files (*.xml)|*.xml|csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                sourceFiles = openFileDialog.FileNames;          
            }
            DataSet ds = new DataSet();

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
                // Получите список столбцов в текущей таблице
                foreach (DataTable table in ds.Tables)
                {
                    string[] columnNames = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

                    // Выведите названия столбцов в лейбл
                    label1.Text += string.Join(", ", columnNames);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "sqlite files (*.db)|*.db|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                dbFile = openFileDialog.FileName;
            }
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

            DataSet ds = new DataSet();

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
            }
            using (var connection = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
            {
                connection.Open();

                foreach (DataTable table in ds.Tables)
                {
                    var insertCommand = new SQLiteCommand($"INSERT INTO {table.TableName} VALUES ({string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => $"@p{c.Ordinal}"))})", connection);

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        insertCommand.Parameters.Add($"p{i}", DbType.String);
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
    }
}
