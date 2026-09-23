using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using TableDbDesktop.Dialogs;
using TableDbEngine.Models;
using TableDbEngine.Services;

namespace TableDbDesktop
{
    public partial class MainWindow : Window
    {
        private Database _currentDb = new Database("DefaultDatabase");
        private Table? _selectedTable;
        private string? _currentFilePath;
        private readonly JsonDbStorage _storage = new();
        private readonly PatternSearchService _searchService = new();

        public MainWindow()
        {
            InitializeComponent();
            SeedInitialDemoData();
            RefreshTableList();
        }

        private void SeedInitialDemoData()
        {
            var demoTable = new Table("Sensors");
            demoTable.Columns.Add(new Column("ID", DataType.Integer));
            demoTable.Columns.Add(new Column("Impedance", DataType.ComplexInteger));
            demoTable.Columns.Add(new Column("Voltage", DataType.ComplexReal));
            demoTable.Columns.Add(new Column("Tag", DataType.String));

            demoTable.AddRow(new Row(new[] { "1", "3+4i", "12.5+0.5i", "Alpha" }));
            demoTable.AddRow(new Row(new[] { "2", "-3+4i", "10.0-1.2i", "Beta" }));
            demoTable.AddRow(new Row(new[] { "3", "3-2i", "5.0+0.0i", "Gamma" }));
            demoTable.AddRow(new Row(new[] { "4", "15+4i", "220.0+15.3i", "Delta" }));
            demoTable.AddRow(new Row(new[] { "5", "5i", "-4.5i", "Epsilon" }));

            _currentDb.Tables.Add(demoTable);
        }

        private void RefreshTableList()
        {
            TablesListBox.ItemsSource = null;
            TablesListBox.ItemsSource = _currentDb.Tables.Select(t => t.Name).ToList();
            if (_currentDb.Tables.Count > 0)
                TablesListBox.SelectedIndex = 0;
            else
            {
                _selectedTable = null;
                MainDataGrid.ItemsSource = null;
                MainDataGrid.Columns.Clear();
            }
        }

        private void TablesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TablesListBox.SelectedIndex >= 0 && TablesListBox.SelectedIndex < _currentDb.Tables.Count)
            {
                _selectedTable = _currentDb.Tables[TablesListBox.SelectedIndex];
                BindDataGrid(_selectedTable.Rows);
                UpdateSearchColumns();
            }
        }

        private void BindDataGrid(List<Row> rowsToDisplay)
        {
            if (_selectedTable == null) return;

            MainDataGrid.Columns.Clear();
            for (int i = 0; i < _selectedTable.Columns.Count; i++)
            {
                var col = _selectedTable.Columns[i];
                int colIdx = i;
                MainDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = $"{col.Name} ({col.Type})",
                    Binding = new Avalonia.Data.Binding($"[{colIdx}]"),
                    IsReadOnly = true
                });
            }

            var displayRows = new List<string[]>();
            foreach (var r in rowsToDisplay)
            {
                var rowArr = new string[_selectedTable.Columns.Count];
                for (int i = 0; i < _selectedTable.Columns.Count; i++)
                    rowArr[i] = i < r.Cells.Count ? r.Cells[i].RawValue : "";
                displayRows.Add(rowArr);
            }

            MainDataGrid.ItemsSource = displayRows;
        }

        private void UpdateSearchColumns()
        {
            if (_selectedTable == null || _selectedTable.Columns.Count == 0)
            {
                SearchColumnComboBox.ItemsSource = null;
                return;
            }

            var list = new List<string> { "(Всі колонки)" };
            list.AddRange(_selectedTable.Columns.Select(c => c.Name));
            SearchColumnComboBox.ItemsSource = list;

            int complexIdx = _selectedTable.Columns.FindIndex(c => c.Type == DataType.ComplexInteger);
            SearchColumnComboBox.SelectedIndex = complexIdx != -1 ? complexIdx + 1 : 0;
        }

        private void Search_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable == null || SearchColumnComboBox.SelectedItem == null) return;
            string selectedOption = SearchColumnComboBox.SelectedItem.ToString()!;
            string pattern = PatternTextBox.Text?.Trim() ?? "";

            try
            {
                List<Row> filtered;

                if (selectedOption == "(Всі колонки)")
                {
                    var matchedRows = new HashSet<Row>();
                    foreach (var col in _selectedTable.Columns)
                    {
                        var colMatches = _searchService.Search(_selectedTable, col.Name, pattern);
                        foreach (var row in colMatches)
                        {
                            matchedRows.Add(row);
                        }
                    }

                    filtered = _selectedTable.Rows.Where(r => matchedRows.Contains(r)).ToList();
                }
                else
                {
                    filtered = _searchService.Search(_selectedTable, selectedOption, pattern);
                }

                BindDataGrid(filtered);
            }
            catch (Exception ex)
            {
                _ = ShowSimpleAlert("Помилка пошуку", ex.Message);
            }
        }

        private void ResetSearch_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable != null) BindDataGrid(_selectedTable.Rows);
        }

        private async void AddTable_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new CreateTableDialog();
            var result = await dialog.ShowDialog<Table?>(this);
            if (result == null) return;

            if (_currentDb.Tables.Any(t => t.Name.Equals(result.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await ShowSimpleAlert("Помилка валідації", $"Таблиця з назвою '{result.Name}' уже існує в базі даних!");
                return;
            }

            _currentDb.Tables.Add(result);
            RefreshTableList();
            TablesListBox.SelectedIndex = _currentDb.Tables.Count - 1;
        }

        private async void DeleteTable_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable == null) return;

            bool confirmed = await ShowConfirmDialog(
                "Підтвердження видалення",
                $"Ви дійсно бажаєте безповоротно видалити таблицю '{_selectedTable.Name}' разом з усіма даними ({_selectedTable.Rows.Count} рядків)?"
            );

            if (confirmed)
            {
                _currentDb.Tables.Remove(_selectedTable);
                RefreshTableList();
            }
        }

        private async void AddColumn_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable == null) return;

            var dialog = new AddColumnDialog();
            var result = await dialog.ShowDialog<AddColumnResult?>(this);
            if (result == null) return;

            if (_selectedTable.Columns.Any(c => c.Name.Equals(result.Column.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await ShowSimpleAlert("Помилка валідації", $"Колонка з назвою '{result.Column.Name}' уже існує в таблиці '{_selectedTable.Name}'!");
                return;
            }

            _selectedTable.Columns.Add(result.Column);
            foreach (var r in _selectedTable.Rows)
                r.Cells.Add(new Cell(result.DefaultValue));

            BindDataGrid(_selectedTable.Rows);
            UpdateSearchColumns();
        }

        private async void EditColumn_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable == null || _selectedTable.Columns.Count == 0)
            {
                await ShowSimpleAlert("Увага", "У поточній таблиці немає колонок для редагування.");
                return;
            }

            var dialog = new Window
            {
                Title = "Редагувати колонку",
                Width = 380,
                Height = 290,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var root = new StackPanel { Margin = new Avalonia.Thickness(15), Spacing = 8 };

            root.Children.Add(new TextBlock { Text = "Оберіть колонку:", FontWeight = FontWeight.SemiBold });
            var selectCombo = new ComboBox
            {
                ItemsSource = _selectedTable.Columns.Select(c => $"{c.Name} ({c.Type})").ToList(),
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            root.Children.Add(selectCombo);

            root.Children.Add(new TextBlock { Text = "Нова назва колонки:", FontWeight = FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 6, 0, 0) });
            var nameBox = new TextBox { Text = _selectedTable.Columns[0].Name };
            root.Children.Add(nameBox);

            root.Children.Add(new TextBlock { Text = "Новий тип даних:", FontWeight = FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 6, 0, 0) });
            var typeCombo = new ComboBox
            {
                ItemsSource = Enum.GetValues(typeof(DataType)),
                SelectedItem = _selectedTable.Columns[0].Type,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            root.Children.Add(typeCombo);

            selectCombo.SelectionChanged += (_, _) =>
            {
                int idx = selectCombo.SelectedIndex;
                if (idx >= 0 && idx < _selectedTable.Columns.Count)
                {
                    nameBox.Text = _selectedTable.Columns[idx].Name;
                    typeCombo.SelectedItem = _selectedTable.Columns[idx].Type;
                }
            };

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10, Margin = new Avalonia.Thickness(0, 15, 0, 0) };
            var btnSave = new Button { Content = "Зберегти", Width = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            var btnCancel = new Button { Content = "Скасувати", Width = 90, HorizontalContentAlignment = HorizontalAlignment.Center };

            btnCancel.Click += (_, _) => dialog.Close();

            btnSave.Click += async (_, _) =>
            {
                int colIdx = selectCombo.SelectedIndex;
                if (colIdx < 0 || colIdx >= _selectedTable.Columns.Count) return;

                var targetCol = _selectedTable.Columns[colIdx];
                string newName = nameBox.Text?.Trim() ?? "";
                var newType = (DataType)typeCombo.SelectedItem!;

                if (string.IsNullOrWhiteSpace(newName))
                {
                    await ShowSimpleAlert("Помилка", "Назва колонки не може бути порожньою.");
                    return;
                }

                bool nameExists = _selectedTable.Columns
                    .Where((c, i) => i != colIdx)
                    .Any(c => c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

                if (nameExists)
                {
                    await ShowSimpleAlert("Помилка", $"Колонка з назвою '{newName}' уже існує в таблиці.");
                    return;
                }

                if (newType != targetCol.Type)
                {
                    for (int r = 0; r < _selectedTable.Rows.Count; r++)
                    {
                        var row = _selectedTable.Rows[r];
                        if (colIdx < row.Cells.Count)
                        {
                            string currentVal = row.Cells[colIdx].RawValue;
                            if (!string.IsNullOrEmpty(currentVal) && !TypeValidator.Validate(currentVal, newType))
                            {
                                await ShowSimpleAlert("Помилка несумісності типів",
                                    $"Неможливо змінити тип на {newType}!\n" +
                                    $"Значення '{currentVal}' у рядку #{r + 1} не відповідає новому типу.");
                                return;
                            }
                        }
                    }
                }

                targetCol.Name = newName;
                targetCol.Type = newType;
                dialog.Close();
            };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            root.Children.Add(btnPanel);
            dialog.Content = root;

            await dialog.ShowDialog(this);
            BindDataGrid(_selectedTable.Rows);
            UpdateSearchColumns();
        }

        private async void DeleteColumn_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable == null || _selectedTable.Columns.Count == 0)
            {
                await ShowSimpleAlert("Увага", "У поточній таблиці немає колонок для видалення.");
                return;
            }

            // Забороняємо видалення єдиної колонки
            if (_selectedTable.Columns.Count == 1)
            {
                await ShowSimpleAlert("Помилка схеми", "Неможливо видалити єдину колонку!\nТаблиця повинна містити щонайменше одну колонку. Якщо таблиця не потрібна, скористайтеся кнопкою '- Видалити таблицю'.");
                return;
            }

            var dialog = new Window
            {
                Title = "Видалити колонку",
                Width = 360,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var root = new StackPanel { Margin = new Avalonia.Thickness(15), Spacing = 10 };
            root.Children.Add(new TextBlock { Text = "Оберіть колонку для видалення:", FontWeight = FontWeight.SemiBold });

            var combo = new ComboBox
            {
                ItemsSource = _selectedTable.Columns.Select(c => c.Name).ToList(),
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            root.Children.Add(combo);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
            var btnDelete = new Button { Content = "Видалити", Width = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            var btnCancel = new Button { Content = "Скасувати", Width = 90, HorizontalContentAlignment = HorizontalAlignment.Center };

            int chosenIndex = -1;
            btnCancel.Click += (_, _) => dialog.Close();
            btnDelete.Click += (_, _) =>
            {
                chosenIndex = combo.SelectedIndex;
                dialog.Close();
            };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnDelete);
            root.Children.Add(btnPanel);
            dialog.Content = root;

            await dialog.ShowDialog(this);

            if (chosenIndex < 0 || chosenIndex >= _selectedTable.Columns.Count) return;

            string targetColName = _selectedTable.Columns[chosenIndex].Name;

            if (_selectedTable.Rows.Count > 0)
            {
                bool confirmed = await ShowConfirmDialog(
                    "Попередження про втрату даних",
                    $"У таблиці є {_selectedTable.Rows.Count} записів. Видалення колонки '{targetColName}' призведе до видалення всіх її збережених значень. Продовжити?"
                );

                if (!confirmed) return;
            }

            _selectedTable.Columns.RemoveAt(chosenIndex);
            foreach (var r in _selectedTable.Rows)
            {
                if (chosenIndex < r.Cells.Count)
                    r.Cells.RemoveAt(chosenIndex);
            }

            BindDataGrid(_selectedTable.Rows);
            UpdateSearchColumns();
        }

        private async void AddRow_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable == null || _selectedTable.Columns.Count == 0) return;

            var dialog = new RowEditorDialog(_selectedTable);
            var values = await dialog.ShowDialog<List<string>?>(this);
            if (values == null) return;

            _selectedTable.AddRow(new Row(values));
            BindDataGrid(_selectedTable.Rows);
        }

        private async void EditRow_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable == null) return;
            int index = MainDataGrid.SelectedIndex;
            if (index < 0 || index >= _selectedTable.Rows.Count) return;

            var existingRow = _selectedTable.Rows[index];
            var dialog = new RowEditorDialog(_selectedTable, existingRow);
            var values = await dialog.ShowDialog<List<string>?>(this);
            if (values == null) return;

            for (int i = 0; i < values.Count && i < existingRow.Cells.Count; i++)
                existingRow.Cells[i].RawValue = values[i];

            BindDataGrid(_selectedTable.Rows);
        }

        private void MainDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
        {
            EditRow_Click(sender, new RoutedEventArgs());
        }

        private async void DeleteRow_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedTable != null && MainDataGrid.SelectedIndex >= 0 && MainDataGrid.SelectedIndex < _selectedTable.Rows.Count)
            {
                _selectedTable.RemoveRowAt(MainDataGrid.SelectedIndex);
                BindDataGrid(_selectedTable.Rows);
            }
            else
            {
                await ShowSimpleAlert("Увага", "Оберіть рядок для видалення у списку.");
            }
        }

        private void NewDatabase_Click(object? sender, RoutedEventArgs e)
        {
            _currentDb = new Database("NewDatabase");
            _currentFilePath = null;
            RefreshTableList();
        }

        private async void SaveDatabase_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsDatabase_Click(sender, e);
                return;
            }

            try
            {
                _storage.Save(_currentDb, _currentFilePath);
                await ShowSimpleAlert("Успіх", $"Зміни успішно збережено у файл:\n{_currentFilePath}");
            }
            catch (Exception ex)
            {
                await ShowSimpleAlert("Помилка збереження", ex.Message);
            }
        }

        private async void SaveAsDatabase_Click(object? sender, RoutedEventArgs e)
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider != null)
            {
                var jsonFilter = new FilePickerFileType("База даних JSON (*.json)")
                {
                    Patterns = new[] { "*.json" },
                    MimeTypes = new[] { "application/json" }
                };

                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Зберегти базу даних як",
                    DefaultExtension = "json",
                    FileTypeChoices = new[] { jsonFilter }
                });

                if (file != null)
                {
                    _currentFilePath = file.Path.LocalPath;
                    _storage.Save(_currentDb, _currentFilePath);
                    await ShowSimpleAlert("Успіх", "Базу даних успішно збережено!");
                }
            }
        }

        private async void OpenDatabase_Click(object? sender, RoutedEventArgs e)
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider != null)
            {
                var jsonFilter = new FilePickerFileType("База даних JSON (*.json)")
                {
                    Patterns = new[] { "*.json" },
                    MimeTypes = new[] { "application/json" }
                };

                var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Відкрити базу даних",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { jsonFilter } 
                });

                if (files.Count > 0)
                {
                    try
                    {
                        _currentFilePath = files[0].Path.LocalPath;
                        _currentDb = _storage.Load(_currentFilePath);
                        RefreshTableList();
                        await ShowSimpleAlert("Успіх", $"Базу даних успішно завантажено з файлу:\n{_currentFilePath}");
                    }
                    catch (Exception ex)
                    {
                        await ShowSimpleAlert("Помилка завантаження", $"Не вдалося прочитати файл бази даних:\n{ex.Message}");
                    }
                }
            }
        }

        private async Task ShowSimpleAlert(string title, string message)
        {
            var alert = new Window
            {
                Title = title,
                Width = 360,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var stack = new StackPanel { Margin = new Avalonia.Thickness(15), Spacing = 12 };
            stack.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });

            var btn = new Button
            {
                Content = "OK",
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            btn.Click += (_, _) => alert.Close();
            stack.Children.Add(btn);

            alert.Content = stack;
            await alert.ShowDialog(this);
        }

        private async Task<bool> ShowConfirmDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 380,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            bool result = false;
            var stack = new StackPanel { Margin = new Avalonia.Thickness(15), Spacing = 12 };
            stack.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            };

            var btnNo = new Button { Content = "Скасувати", Width = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            var btnYes = new Button { Content = "Так, видалити", Width = 110, HorizontalContentAlignment = HorizontalAlignment.Center };

            btnNo.Click += (_, _) => dialog.Close();
            btnYes.Click += (_, _) =>
            {
                result = true;
                dialog.Close();
            };

            btnPanel.Children.Add(btnNo);
            btnPanel.Children.Add(btnYes);
            stack.Children.Add(btnPanel);

            dialog.Content = stack;
            await dialog.ShowDialog(this);
            return result;
        }
    }
}