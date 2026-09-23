using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TableDbEngine.Models;

namespace TableDbDesktop.Dialogs
{
    public partial class CreateTableDialog : Window
    {
        public ObservableCollection<Column> PendingColumns { get; } = new();

        public CreateTableDialog()
        {
            InitializeComponent();
            ColumnTypeComboBox.ItemsSource = Enum.GetValues(typeof(DataType));
            ColumnTypeComboBox.SelectedIndex = 0;
            ColumnsListBox.ItemsSource = PendingColumns;
        }

        private void AddColumn_Click(object? sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            var name = NewColumnNameTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Введіть назву колонки.";
                return;
            }
            if (PendingColumns.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorText.Text = "Колонка з такою назвою вже існує.";
                return;
            }
            if (ColumnTypeComboBox.SelectedItem is not DataType type)
            {
                ErrorText.Text = "Оберіть тип колонки.";
                return;
            }

            PendingColumns.Add(new Column(name, type));
            NewColumnNameTextBox.Text = "";
        }

        private void RemoveColumn_Click(object? sender, RoutedEventArgs e)
        {
            if (ColumnsListBox.SelectedItem is Column col)
                PendingColumns.Remove(col);
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            var name = TableNameTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Введіть назву таблиці.";
                return;
            }
            if (PendingColumns.Count == 0)
            {
                ErrorText.Text = "Додайте хоча б одну колонку.";
                return;
            }

            var table = new Table(name);
            table.Columns.AddRange(PendingColumns);
            Close(table);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
    }
}