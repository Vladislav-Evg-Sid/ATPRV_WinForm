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
using System.Collections;
using System.Diagnostics;
using System.Data.SqlTypes;

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
            for (int i=0; i<words.Length; i++)
            {
                table.Rows.Add(words[i], 0);
                words[i] = words[i];
            }
            string filePath = "Путь к файлам с записями"; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! ВПИШИ СВОЁ
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
                        //Parallel.For(0, lines.Length, async (l) =>
                        for (int l=0; l<lines.Length; l++)
                        {
                            string line = lines[l];
                            for (int w = 0; w < words.Length; w++)
                            {
                                int index = 0;
                                int cnt = 0;
                                while ((index = line.IndexOf(words[w], index)) != -1)
                                {
                                    cnt++;
                                    index += words[w].Length; // Перемещаемся на длину подстроки вперед
                                }
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
            this.button1.Enabled = false;
            EX1();
            this.button1.Enabled = true;
        }

        private CancellationTokenSource cancellationTokenSource;
        private bool finished = false;

        private async void startButton_Click(object sender, EventArgs e)
        {
            string url = urlTextBox.Text;
            int depth;
            string path = pathTextBox.Text;

            if (!int.TryParse(depthTextBox.Text, out depth))
            {
                MessageBox.Show("Пожалуйста, введите корректное значение глубины.");
                return;
            }

            startButton.Enabled = false;
            finished = false; // Сбрасываем состояние завершения перед началом

            // Инициализируем CancellationTokenSource
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                Task loadingTask = Task.Run(() => StartLoading(token));
                Crawler crawler = new Crawler(url, depth);
                Task crawlingTask = crawler.Crawl();

                // Ожидаем завершения задачи Crawl
                await crawlingTask;

                Console.WriteLine("Ending Crawling...");

                crawler.SaveToJson(path);

                Console.WriteLine("Ending saving...");

                // Устанавливаем флаг завершения и отменяем загрузку
                finished = true;
                cancellationTokenSource.Cancel();

                // Ожидаем завершения загрузки
                await loadingTask;
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Загрузка была отменена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                startButton.Enabled = true;
                loadingLabel.Text = String.Empty; // Сброс текста лейбла после завершения
            }
        }

        private void StartLoading(CancellationToken token)
        {
            loadingLabel.AutoSize = false;

            int counter = 0;
            string loadingString = "Загрузка";

            while (!token.IsCancellationRequested) // Проверяем на запрос отмены
            {
                Thread.Sleep(1000); // Имитация работы

                if (counter == 3)
                {
                    loadingString = "Загрузка";
                    counter = 0;
                }

                loadingString += ".";
                counter++;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<Class1.Employee> employees = Class1.Generate(1000);

            var firstEmployees = employees.Take(100).ToList();

            dataGridView2.DataSource = firstEmployees.Select(em => new
            {
                em.Name,
                OrdersCount = em.Orders.Count()

            }).ToList();

            var firstOrders = employees.SelectMany(em => em.Orders).Take(500).ToList();

            dataGridView3.DataSource = firstOrders;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var filteredOrdersPlinq = employees
                                    .AsParallel()
                                    .Where(em => em.Name.Contains("Fillton") || em.Name.StartsWith("A"))
                                    .SelectMany(em => em.Orders)
                                    .ToList();
            sw.Stop();
            long mils1 = sw.ElapsedMilliseconds;
            label5.Text = mils1.ToString();
            sw.Restart();

            dataGridView4.DataSource = filteredOrdersPlinq.Select(o => new
            {
                o.Date,
                o.Summ
            }).ToList();

            DateTime filterDate = new DateTime(2024, 1, 1);

            sw.Start();

            var ordersBeforeDatePlinq = employees
                                        .AsParallel()
                                        .SelectMany(em => em.Orders)
                                        .Where(o => o.Date < filterDate)
                                        .ToList();

            sw.Stop();
            long mils2 = sw.ElapsedMilliseconds;
            label6.Text = mils2.ToString();
            sw.Restart();

            dataGridView4.DataSource = ordersBeforeDatePlinq.Select(o => new
            {
                o.Date,
                o.Summ
            }).ToList();

            sw.Start();

            var employeesSortedByAveragePlinq = employees
                                            .AsParallel()
                                            .Select(em => new
                                            {
                                                em.Name,
                                                AverageOrderSum = em.Orders.Average(o => o.Summ)
                                            })
                                            .OrderByDescending(em => em.AverageOrderSum)
                                            .ToList();

            sw.Stop();
            long mils3 = sw.ElapsedMilliseconds;
            label7.Text = mils3.ToString();
            sw.Restart();

            dataGridView4.DataSource = employeesSortedByAveragePlinq;
        }
    }
}
