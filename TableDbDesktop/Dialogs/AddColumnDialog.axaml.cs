using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TableDbEngine.Models;
using TableDbEngine.Services;

namespace TableDbDesktop.Dialogs
{
    public class AddColumnResult
    {
        public Column Column { get; set; } = null!;
        public string DefaultValue { get; set; } = "";
    }

    public partial class AddColumnDialog : Window
    {
        public AddColumnDialog()
        {
            InitializeComponent();
            TypeComboBox.ItemsSource = Enum.GetValues(typeof(DataType));
            TypeComboBox.SelectedIndex = 0;
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";
            var name = NameTextBox.Text?.Trim();
            var defaultVal = DefaultValueTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Введіть назву колонки.";
                return;
            }

            if (TypeComboBox.SelectedItem is not DataType type)
            {
                ErrorText.Text = "Оберіть тип колонки.";
                return;
            }

            if (string.IsNullOrEmpty(defaultVal))
            {
                defaultVal = type switch
                {
                    DataType.Integer => "0",
                    DataType.Real => "0.0",
                    DataType.Char => "-",
                    DataType.String => "",
                    DataType.ComplexInteger => "0+0i",
                    DataType.ComplexReal => "0.0+0.0i",
                    _ => ""
                };
            }
            else
            {
                if (!TypeValidator.Validate(defaultVal, type))
                {
                    ErrorText.Text = $"Введене значення за замовчуванням не відповідає типу {type}.";
                    return;
                }
            }

            Close(new AddColumnResult { Column = new Column(name, type), DefaultValue = defaultVal });
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
    }
}