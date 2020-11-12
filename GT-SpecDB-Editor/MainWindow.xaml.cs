using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using GT_SpecDB_Editor.Core;
using GT_SpecDB_Editor.Mapping;
using GT_SpecDB_Editor.Mapping.Types;

namespace GT_SpecDB_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public SpecDB CurrentDatabase { get; set; }
        public SpecDBTable CurrentTable { get; set; }

        private string _filterString;
        public string FilterString
        {
            get { return _filterString; }
            set
            {
                _filterString = value;
                NotifyPropertyChanged("FilterString");
                FilterCollection();
            }
        }

        private ICollectionView _dataGridCollection;
        private void FilterCollection()
        {
            if (_dataGridCollection != null)
            {
                _dataGridCollection.Refresh();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            dg_Rows.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new Binding("ID"),
            });

            dg_Rows.Columns.Add(new DataGridTextColumn
            {
                Header = "Label",
                Binding = new Binding("Label"),
            });


        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string property)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness(0);
            }
        }

        private void mi_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenSpecDB_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog("Open SpecDB");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.IsFolderPicker = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CurrentDatabase = SpecDB.LoadFromSpecDBFolder(dlg.FileName, false);
                lb_Tables.Items.Clear();
                foreach (var table in CurrentDatabase.Tables)
                {
                    lb_Tables.Items.Add(table.Key);
                }
            }
        }

        private void SaveCurrentTable_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog("Select folder to save the table");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.IsFolderPicker = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CurrentTable.SaveTable(CurrentDatabase, dlg.FileName);
            }
        }

        private async void lb_Tables_Selected(object sender, SelectionChangedEventArgs e)
        {
            CurrentTable = CurrentDatabase.Tables[(string)lb_Tables.SelectedItem];

            if (!CurrentTable.IsLoaded)
            {
                progressName.Text = $"Loading {CurrentTable.TableName}..";
                progressBar.IsEnabled = true;
                progressBar.IsIndeterminate = true;
                try
                {
                    var loadTask = Task.Run(() =>
                    {
                        CurrentTable.LoadAllRows(CurrentDatabase);
                    });
                    await loadTask;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not load table: {ex.Message}", "Table not loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SetNoProgress();
                    return;
                }
            }
            
            PopulateTableColumnsAndRows();

            SetNoProgress();
            SetupFilters();
            btn_AddRow.IsEnabled = true;
            btn_DeleteRow.IsEnabled = true;
            btn_CopyRow.IsEnabled = true;
            mi_SaveTable.IsEnabled = true;
            tb_ColumnFilter.IsEnabled = true;
        }

        private void btn_AddRow_Click(object sender, RoutedEventArgs e)
        {
            var newRow = new SpecDBRowData();
            newRow.ID = ++CurrentTable.LastID;
            CurrentTable.Rows.Add(newRow);

            foreach (var colMeta in CurrentTable.TableMetadata.Columns)
            {
                switch (colMeta.ColumnType)
                {
                    case DBColumnType.Bool:
                        newRow.ColumnData.Add(new DBBool(false)); break;
                    case DBColumnType.Byte:
                        newRow.ColumnData.Add(new DBByte(0)); break;
                    case DBColumnType.Short:
                        newRow.ColumnData.Add(new DBShort(0)); break;
                    case DBColumnType.Int:
                        newRow.ColumnData.Add(new DBInt(0)); break;
                    case DBColumnType.Long:
                        newRow.ColumnData.Add(new DBLong(0)); break;
                    case DBColumnType.Float:
                        newRow.ColumnData.Add(new DBFloat(0)); break;
                    case DBColumnType.String:
                        newRow.ColumnData.Add(new DBString(0, colMeta.StringFileName)); break;
                    default:
                        break;
                }
            }
            
        }

        private void dg_Rows_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var currentRow = e.Row.Item as SpecDBRowData;

            if ((e.EditingElement as TextBox) is null)
                return;

            var newInput = (e.EditingElement as TextBox).Text;
            if (e.Column.Header.Equals("ID"))
            {
                if (int.TryParse(newInput, out int newValue))
                {
                    if (CurrentTable.Rows.Any(row => row.ID == newValue && row != currentRow))
                    {
                        MessageBox.Show("This ID is already being used by another row.", "Validation Error");
                        currentRow.ID = CurrentTable.LastID + 1;
                        e.Cancel = true;
                        return;
                    }

                    var nextRow = CurrentTable.Rows.FirstOrDefault(r => r.ID >= newValue);
                    currentRow.ID = newValue;

                    if (nextRow is null) // End of list?
                    {
                        CurrentTable.Rows.Move(CurrentTable.Rows.IndexOf(currentRow), CurrentTable.Rows.Count - 1);
                        return; 
                    }

                    var nextRowIndex = CurrentTable.Rows.IndexOf(nextRow);
                    if (nextRowIndex > CurrentTable.Rows.IndexOf(currentRow)) // If the row is being moved backwards
                        nextRowIndex--;

                    CurrentTable.Rows.Move(CurrentTable.Rows.IndexOf(currentRow), nextRowIndex);
                }
                else
                {
                    currentRow.ID = CurrentTable.LastID + 1;
                    e.Cancel = true;
                }
            }
            else if (e.Column.Header.Equals("Label"))
            {
                if (CurrentTable.Rows.Any(row => row.Label != null && row.Label.Equals(newInput) && row != currentRow))
                {
                    MessageBox.Show("This Label is already being used by another row.", "Validation Error");
                    currentRow.Label = string.Empty;
                    e.Cancel = true;
                }
                else if (!newInput.All(c => char.IsLetterOrDigit(c) || c.Equals('_')))
                {
                    MessageBox.Show("The label must contain only letters, numbers, or underscores.", "Validation Error");
                    currentRow.Label = string.Empty;
                    e.Cancel = true;
                }
                currentRow.Label = newInput;
            }
            else
            {
                // Perform regular validation
                var dataCol = CurrentTable.TableMetadata.Columns.Find(col => col.ColumnName == (string)e.Column.Header);
                if (dataCol.ColumnType == DBColumnType.Int)
                {
                    if (!int.TryParse(newInput, out int res))
                    {
                        MessageBox.Show($"Could not parse 'Integer' type. It must be number between {int.MinValue} and {int.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.Short)
                {
                    if (!int.TryParse(newInput, out int res))
                    {
                        MessageBox.Show($"Could not parse 'Short' type. It must be number between {short.MinValue} and {short.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.Byte)
                {
                    if (!int.TryParse(newInput, out int res))
                    {
                        MessageBox.Show($"Could not parse 'Byte' type. It must be a number between {byte.MinValue} and {byte.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.Float)
                {
                    if (!float.TryParse(newInput, out float res))
                    {
                        MessageBox.Show($"Could not parse 'Float' type. It must be a number between {float.MinValue} and {float.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.Long)
                {
                    if (!long.TryParse(newInput, out long res))
                    {
                        MessageBox.Show($"Could not parse 'Long' type. It must be a number between {long.MinValue} and {long.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
            }
        }

        private void dg_Rows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is DataGridCell cell))
                return;

            var dataCol = CurrentTable.TableMetadata.Columns.Find(col => col.ColumnName == (string)cell.Column.Header);
            if (dataCol is null)
                return;

            if (dataCol.ColumnType == DBColumnType.String)
            {
                var strWindow = new StringDatabaseManager(CurrentDatabase.StringDatabases[dataCol.StringFileName]);
                strWindow.ShowDialog();
                if (strWindow.HasSelected)
                {
                    // Find column index to apply our row data to
                    var row = cell.DataContext as SpecDBRowData;
                    int columnIndex = CurrentTable.TableMetadata.Columns.IndexOf(dataCol);

                    // Apply string change
                    var str = row.ColumnData[columnIndex] as DBString;
                    str.StringIndex = strWindow.SelectedString.index;
                    str.Value = strWindow.SelectedString.selectedString;
                }
            }
        }


        private void btn_DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Rows.SelectedIndex == -1)
                return;

            CurrentTable.Rows.Remove(CurrentTable.Rows[dg_Rows.SelectedIndex]);
            CurrentTable.LastID = CurrentTable.Rows.Max(row => row.ID);
        }

        private void btn_CopyRow_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Rows.SelectedIndex == -1)
                return;

            var selectedRow = CurrentTable.Rows[dg_Rows.SelectedIndex];

            var newRow = new SpecDBRowData();
            newRow.ID = ++CurrentTable.LastID;
            newRow.Label = $"{selectedRow.Label}_copy";
            CurrentTable.Rows.Add(newRow);

            for (int i = 0; i < CurrentTable.TableMetadata.Columns.Count; i++)
            {
                ColumnMetadata colMeta = CurrentTable.TableMetadata.Columns[i];
                switch (colMeta.ColumnType)
                {
                    case DBColumnType.Bool:
                        newRow.ColumnData.Add(new DBBool(((DBBool)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.Byte:
                        newRow.ColumnData.Add(new DBByte(((DBByte)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.Short:
                        newRow.ColumnData.Add(new DBShort(((DBShort)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.Int:
                        newRow.ColumnData.Add(new DBInt(((DBInt)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.Float:
                        newRow.ColumnData.Add(new DBFloat(((DBFloat)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.Long:
                        newRow.ColumnData.Add(new DBLong(((DBLong)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.String:
                        var str = new DBString(((DBString)selectedRow.ColumnData[i]).StringIndex, colMeta.StringFileName);
                        str.Value = CurrentDatabase.StringDatabases[colMeta.StringFileName].Strings[str.StringIndex];
                        newRow.ColumnData.Add(str); 
                        break;
                    default:
                        break;
                }
            }
        }

        private void cm_DumpTable_Click(object sender, RoutedEventArgs e)
        {
            if (lb_Tables.SelectedIndex == -1)
                return;

            SaveFileDialog dlg = new SaveFileDialog();
            if (dlg.ShowDialog() == true)
            {
                var table = CurrentDatabase.Tables[(string)lb_Tables.SelectedItem];
                int rows = table.DumpTable(dlg.FileName);
                MessageBox.Show($"Dumped table with {rows} rows at {dlg.FileName}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void tb_ColumnFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb_ColumnFilter.Text.Length != 1)
                FilterString = tb_ColumnFilter.Text;
        }

        public void SetupFilters()
        {
            _dataGridCollection = CollectionViewSource.GetDefaultView(dg_Rows.ItemsSource);
            if (_dataGridCollection != null)
            {
                _dataGridCollection.Filter = FilterTask;
            }
        }

        public bool FilterTask(object value)
        {
            if (string.IsNullOrEmpty(FilterString) || FilterString.Length < 2)
                return true;

            if (value is SpecDBRowData row && row.ColumnData.Count != 0)
                return row.Label.Contains(FilterString);

            return false;
        }

        private void PopulateTableColumnsAndRows()
        {
            dg_Rows.ItemsSource = null;
            for (int i = dg_Rows.Columns.Count - 1; i >= 2; i--)
                dg_Rows.Columns.Remove(dg_Rows.Columns[i]);

            for (int i = 0; i < CurrentTable.TableMetadata.Columns.Count; i++)
            {
                ColumnMetadata column = CurrentTable.TableMetadata.Columns[i];
                if (column.ColumnType == DBColumnType.Bool)
                {
                    dg_Rows.Columns.Add(new DataGridCheckBoxColumn
                    {
                        Header = column.ColumnName,
                        Binding = new Binding($"ColumnData[{i}].Value"),
                    });
                }
                else
                {
                    dg_Rows.Columns.Add(new DataGridTextColumn
                    {
                        Header = column.ColumnName,
                        Binding = new Binding($"ColumnData[{i}].Value"),
                        IsReadOnly = column.ColumnType == DBColumnType.String,
                    });
                }
            }

            dg_Rows.ItemsSource = CurrentTable.Rows;
        }

        private void SetNoProgress()
        {
            progressName.Text = "Ready";
            progressBar.IsEnabled = false;
            progressBar.IsIndeterminate = false;
        }
    }
}
