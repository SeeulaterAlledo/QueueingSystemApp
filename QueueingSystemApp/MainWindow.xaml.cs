using Microsoft.Win32;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace QueueingSystemApp
{
    public partial class MainWindow : Window
    {
        private DataTable lastResultTable;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void FillExample_Click(object sender, RoutedEventArgs e)
        {
            LambdaTextBox.Text = "2";
            MuTextBox.Text = "3";
            LogTextBox.Text = "Тестовые данные заполнены.\n";
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParseDouble(LambdaTextBox.Text, out double lambda) || lambda <= 0)
                {
                    MessageBox.Show("Введите корректное значение λ (интенсивность поступления).");
                    return;
                }

                if (!TryParseDouble(MuTextBox.Text, out double mu) || mu <= 0)
                {
                    MessageBox.Show("Введите корректное значение μ (интенсивность обслуживания).");
                    return;
                }

                if (lambda >= mu)
                {
                    MessageBox.Show("Система неустойчива: должно выполняться условие λ < μ.");
                    return;
                }

                double rho = lambda / mu;
                double p0 = 1 - rho;
                double lq = (rho * rho) / (1 - rho);
                double l = rho / (1 - rho);
                double wq = lq / lambda;
                double w = l / lambda;

                DataTable dt = new DataTable();
                dt.Columns.Add("Показатель");
                dt.Columns.Add("Значение");
                dt.Columns.Add("Описание");

                AddRow(dt, "λ", lambda, "Интенсивность поступления заявок");
                AddRow(dt, "μ", mu, "Интенсивность обслуживания");
                AddRow(dt, "ρ", rho, "Коэффициент загрузки системы");
                AddRow(dt, "P0", p0, "Вероятность отсутствия заявок в системе");
                AddRow(dt, "Lq", lq, "Среднее число заявок в очереди");
                AddRow(dt, "L", l, "Среднее число заявок в системе");
                AddRow(dt, "Wq", wq, "Среднее время ожидания в очереди");
                AddRow(dt, "W", w, "Среднее время пребывания в системе");

                ResultDataGrid.ItemsSource = dt.DefaultView;
                lastResultTable = dt;

                SummaryTextBlock.Text = $"Итог: система устойчива, ρ = {rho:0.###}";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Расчет показателей СМО M/M/1:");
                sb.AppendLine();
                sb.AppendLine($"Введено λ = {lambda:0.###}");
                sb.AppendLine($"Введено μ = {mu:0.###}");
                sb.AppendLine();
                sb.AppendLine($"ρ = λ / μ = {lambda:0.###} / {mu:0.###} = {rho:0.###}");
                sb.AppendLine($"P0 = 1 - ρ = {p0:0.###}");
                sb.AppendLine($"Lq = ρ² / (1 - ρ) = {lq:0.###}");
                sb.AppendLine($"L = ρ / (1 - ρ) = {l:0.###}");
                sb.AppendLine($"Wq = Lq / λ = {wq:0.###}");
                sb.AppendLine($"W = L / λ = {w:0.###}");
                sb.AppendLine();
                sb.AppendLine("Вывод: система работает устойчиво, так как λ < μ.");

                LogTextBox.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка расчета: " + ex.Message);
            }
        }

        private bool TryParseDouble(string text, out double value)
        {
            text = text.Replace(',', '.');
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private void AddRow(DataTable dt, string name, double value, string description)
        {
            DataRow row = dt.NewRow();
            row["Показатель"] = name;
            row["Значение"] = value.ToString("0.###");
            row["Описание"] = description;
            dt.Rows.Add(row);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lastResultTable == null)
                {
                    MessageBox.Show("Сначала выполните расчет.");
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV file (*.csv)|*.csv",
                    FileName = "queueing_result.csv"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < lastResultTable.Columns.Count; i++)
                {
                    sb.Append(lastResultTable.Columns[i].ColumnName);
                    if (i < lastResultTable.Columns.Count - 1)
                        sb.Append(";");
                }
                sb.AppendLine();

                foreach (DataRow row in lastResultTable.Rows)
                {
                    for (int i = 0; i < lastResultTable.Columns.Count; i++)
                    {
                        sb.Append(row[i]?.ToString());
                        if (i < lastResultTable.Columns.Count - 1)
                            sb.Append(";");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine();
                sb.AppendLine("Лог расчета");
                sb.AppendLine(LogTextBox.Text.Replace(Environment.NewLine, " "));

                File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Экспорт выполнен успешно.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            LambdaTextBox.Clear();
            MuTextBox.Clear();

            ResultDataGrid.ItemsSource = null;
            SummaryTextBlock.Text = "Итог: ";
            LogTextBox.Clear();

            lastResultTable = null;
        }
    }
}