using Microsoft.Win32;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace QueueingSystemApp
{
    public partial class MainWindow : Window
    {
        private DataTable lastResultTable;

        public MainWindow()
        {
            InitializeComponent();
            UpdateModeInfo();
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UpdateModeInfo();
        }

        private void UpdateModeInfo()
        {
            string mode = GetSelectedMode();

            if (mode == "QUEUE")
            {
                FormulaTitleTextBlock.Text = "Режим: одноканальная СМО с очередью";
                FormulaTextBox.Text =
                    "Используемые формулы:\n\n" +
                    "ρ = λ / μ\n" +
                    "P0 = 1 - ρ\n" +
                    "Lq = ρ² / (1 - ρ)\n" +
                    "L = ρ / (1 - ρ)\n" +
                    "Wq = Lq / λ\n" +
                    "W = L / λ\n\n" +
                    "Условие устойчивости:\n" +
                    "λ < μ";
                HintTextBlock.Text = "Для режима с очередью обязательно должно выполняться условие λ < μ.";
            }
            else
            {
                FormulaTitleTextBlock.Text = "Режим: одноканальная СМО с отказами";
                FormulaTextBox.Text =
                    "Используемые формулы:\n\n" +
                    "ρ = λ / μ\n" +
                    "P0 = 1 / (1 + ρ)\n" +
                    "Pотк = ρ / (1 + ρ)\n" +
                    "Q = 1 - Pотк = Pобс\n" +
                    "A = λ · Q\n\n" +
                    "Где:\n" +
                    "P0 — вероятность простоя\n" +
                    "Pотк — вероятность отказа\n" +
                    "Q — относительная пропускная способность\n" +
                    "A — абсолютная пропускная способность";
                HintTextBlock.Text = "Для режима с отказами система считается одноканальной без очереди: если канал занят, заявка получает отказ.";
            }
        }

        private string GetSelectedMode()
        {
            ComboBoxItem selectedItem = ModeComboBox.SelectedItem as ComboBoxItem;
            string text = selectedItem?.Content?.ToString() ?? "";

            if (text.Contains("отказами"))
                return "REFUSAL";

            return "QUEUE";
        }

        private void FillExample_Click(object sender, RoutedEventArgs e)
        {
            string mode = GetSelectedMode();

            if (mode == "QUEUE")
            {
                LambdaTextBox.Text = "2";
                MuTextBox.Text = "3";
                LogTextBox.Text = "Заполнен пример для одноканальной СМО с очередью.\n";
            }
            else
            {
                LambdaTextBox.Text = "16";
                MuTextBox.Text = "25";
                LogTextBox.Text = "Заполнен пример для одноканальной СМО с отказами.\n";
            }
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryParseDouble(LambdaTextBox.Text, out double lambda) || lambda <= 0)
                {
                    MessageBox.Show("Введите корректное значение λ.");
                    return;
                }

                if (!TryParseDouble(MuTextBox.Text, out double mu) || mu <= 0)
                {
                    MessageBox.Show("Введите корректное значение μ.");
                    return;
                }

                string mode = GetSelectedMode();

                if (mode == "QUEUE")
                {
                    CalculateQueueMode(lambda, mu);
                }
                else
                {
                    CalculateRefusalMode(lambda, mu);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка расчета: " + ex.Message);
            }
        }

        private void CalculateQueueMode(double lambda, double mu)
        {
            if (lambda >= mu)
            {
                MessageBox.Show("Для режима с очередью должно выполняться условие λ < μ.");
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
            sb.AppendLine("Расчет для одноканальной СМО с очередью:");
            sb.AppendLine();
            sb.AppendLine($"λ = {lambda:0.###}");
            sb.AppendLine($"μ = {mu:0.###}");
            sb.AppendLine();
            sb.AppendLine($"ρ = λ / μ = {lambda:0.###} / {mu:0.###} = {rho:0.###}");
            sb.AppendLine($"P0 = 1 - ρ = {p0:0.###}");
            sb.AppendLine($"Lq = ρ² / (1 - ρ) = {lq:0.###}");
            sb.AppendLine($"L = ρ / (1 - ρ) = {l:0.###}");
            sb.AppendLine($"Wq = Lq / λ = {wq:0.###}");
            sb.AppendLine($"W = L / λ = {w:0.###}");
            sb.AppendLine();
            sb.AppendLine("Вывод: система устойчива, так как λ < μ.");

            LogTextBox.Text = sb.ToString();
        }

        private void CalculateRefusalMode(double lambda, double mu)
        {
            double rho = lambda / mu;
            double p0 = 1.0 / (1.0 + rho);
            double pReject = rho / (1.0 + rho);
            double q = 1.0 - pReject;
            double a = lambda * q;

            DataTable dt = new DataTable();
            dt.Columns.Add("Показатель");
            dt.Columns.Add("Значение");
            dt.Columns.Add("Описание");

            AddRow(dt, "λ", lambda, "Интенсивность поступления заявок");
            AddRow(dt, "μ", mu, "Интенсивность обслуживания");
            AddRow(dt, "ρ", rho, "Приведенная нагрузка");
            AddRow(dt, "P0", p0, "Вероятность простоя канала");
            AddRow(dt, "Pотк", pReject, "Вероятность отказа");
            AddRow(dt, "Q", q, "Относительная пропускная способность");
            AddRow(dt, "A", a, "Абсолютная пропускная способность");

            ResultDataGrid.ItemsSource = dt.DefaultView;
            lastResultTable = dt;

            SummaryTextBlock.Text = $"Итог: вероятность отказа = {pReject:0.###}, Q = {q:0.###}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Расчет для одноканальной СМО с отказами:");
            sb.AppendLine();
            sb.AppendLine($"λ = {lambda:0.###}");
            sb.AppendLine($"μ = {mu:0.###}");
            sb.AppendLine();
            sb.AppendLine($"ρ = λ / μ = {lambda:0.###} / {mu:0.###} = {rho:0.###}");
            sb.AppendLine($"P0 = 1 / (1 + ρ) = {p0:0.###}");
            sb.AppendLine($"Pотк = ρ / (1 + ρ) = {pReject:0.###}");
            sb.AppendLine($"Q = 1 - Pотк = {q:0.###}");
            sb.AppendLine($"A = λ · Q = {lambda:0.###} · {q:0.###} = {a:0.###}");
            sb.AppendLine();
            sb.AppendLine("Вывод: получены показатели одноканальной СМО без очереди, где занятый канал приводит к отказу.");

            LogTextBox.Text = sb.ToString();
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
                    FileName = "queueing_universal_result.csv"
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