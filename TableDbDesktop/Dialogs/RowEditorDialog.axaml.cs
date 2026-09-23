using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using TableDbEngine.Models;
using TableDbEngine.Services;

namespace TableDbDesktop.Dialogs
{
    public partial class RowEditorDialog : Window
    {
        private readonly Table _table;
        private readonly List<TextBox> _textBoxes = new();
        private readonly List<TextBlock> _errorBlocks = new();

        public RowEditorDialog()
        {
            InitializeComponent();
            _table = new Table("Dummy");
        }

        public RowEditorDialog(Table table, Row? existingRow = null)
        {
            InitializeComponent();
            _table = table;
            Title = existingRow == null ? $"Новий рядок — {table.Name}" : $"Редагувати рядок — {table.Name}";

            BuildDynamicForm(existingRow);
        }

        private void BuildDynamicForm(Row? existingRow)
        {
            FieldsPanel.Children.Clear();
            _textBoxes.Clear();
            _errorBlocks.Clear();

            for (int i = 0; i < _table.Columns.Count; i++)
            {
                var col = _table.Columns[i];

                var label = new TextBlock
                {
                    Text = $"{col.Name} ({col.Type}):",
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Avalonia.Thickness(0, 4, 0, 2)
                };

                string initialValue = "";
                if (existingRow != null && i < existingRow.Cells.Count)
                {
                    initialValue = existingRow.Cells[i].RawValue;
                }

                var textBox = new TextBox
                {
                    Text = initialValue,
                    PlaceholderText = $"Значення типу {col.Type}"
                };

                var errorBlock = new TextBlock
                {
                    Foreground = Brushes.Red,
                    FontSize = 11,
                    Margin = new Avalonia.Thickness(0, 2, 0, 6)
                };

                _textBoxes.Add(textBox);
                _errorBlocks.Add(errorBlock);

                FieldsPanel.Children.Add(label);
                FieldsPanel.Children.Add(textBox);
                FieldsPanel.Children.Add(errorBlock);
            }
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            bool isValid = true;
            var values = new List<string>();

            for (int i = 0; i < _table.Columns.Count; i++)
            {
                var col = _table.Columns[i];
                var raw = _textBoxes[i].Text?.Trim() ?? "";

                if (string.IsNullOrEmpty(raw))
                {
                    raw = col.Type switch
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

                if (!TypeValidator.Validate(raw, col.Type))
                {
                    _errorBlocks[i].Text = $"Некоректне значення для типу {col.Type}.";
                    isValid = false;
                }
                else
                {
                    _errorBlocks[i].Text = "";
                    values.Add(raw);
                }
            }

            if (isValid)
            {
                Close(values);
            }
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}