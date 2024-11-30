using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void EX1()
        {
            string[] words = textBox1.Text.Split(' ');
            DataTable table = new DataTable();
            table.Columns.Add("Слово");
            table.Columns.Add("Количество вхождений");
            foreach (var word in words)
            {
                table.Rows.Add(word, 0);
            }
            string filePath = "E:\\documents\\ВУЗ\\семестр 5\\АТПРВ (распределённые)\\6\\ConsoleApp4\\";
            var ready = new SemaphoreSlim(1);
            Task[] reader = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                if (checkBox[i].Checked)
                {
                    int id = i;
                    reader[id] = Task.Run(async () =>
                    {
                        // Чтение файла по строкам
                        string[] lines = File.ReadLines(filePath + (id + 1) + ".txt").AsParallel().ToArray();

                        // Вывод обработанных строк
                        for (int l = 0; l < lines.Length; l++)
                        {
                            string line = lines[l];
                            for (int w = 0; w < words.Length; w++)
                            {
                                int cnt = line.IndexOf(words[w]);
                                if (cnt > 0)
                                {
                                    await ready.WaitAsync();
                                    table.Rows[w][1] = Convert.ToInt32(table.Rows[w][1]) + cnt;
                                    ready.Release();
                                }
                            }
                                Console.WriteLine(line);
                        }
                    });
                }
            }
            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = table;
        }

        private void Ex1_ButtonClick(object sender, EventArgs e)
        {
            EX1();
        }
    }
}
