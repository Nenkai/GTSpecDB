using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.ComponentModel;

using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;

using Humanizer;

using GT_SpecDB_Editor.Core;
using GT_SpecDB_Editor.Utils;
using GT_SpecDB_Editor.Mapping;
using GT_SpecDB_Editor.Mapping.Types;

namespace GT_SpecDB_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public const string WindowTitle = "Gran Turismo Spec Database Editor";

        public SpecDB CurrentDatabase { get; set; }
        public SpecDBTable CurrentTable { get; set; }
        private string _filterString;
        public string FilterString
        {
            get => _filterString;
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

            cb_FilterColumnType.Items.Add("ID");
            cb_FilterColumnType.Items.Add("Label");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string property)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            var toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
                overflowGrid.Visibility = Visibility.Collapsed;

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
                mainPanelBorder.Margin = new Thickness(0);
        }

        private void mi_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenSpecDB_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog("Open SpecDB");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.IsFolderPicker = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    CurrentDatabase = SpecDB.LoadFromSpecDBFolder(dlg.FileName, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not load SpecDB: {ex.Message}", "Failed to load the SpecDB", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CurrentTable = null;
                dg_Rows.ItemsSource = null;
                tb_ColumnFilter.Text = "";
                FilterString = "";
                mi_SavePartsInfo.IsEnabled = true;
                lb_Tables.Items.Clear();
                foreach (var table in CurrentDatabase.Tables)
                    lb_Tables.Items.Add(table.Key);

                this.Title = $"{WindowTitle} - {CurrentDatabase.SpecDBFolderType} [{CurrentDatabase.SpecDBFolderType.Humanize()}]";
            }
        }

        private async void SavePartsInfo_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog("Select folder to save PartsInfo.tbd/tbi");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.IsFolderPicker = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (!CurrentDatabase.Tables.TryGetValue("GENERIC_CAR", out SpecDBTable genericCar))
                {
                    MessageBox.Show($"Can not save PartsInfo as GENERIC_CAR is missing.", "Table not loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!genericCar.IsLoaded)
                    genericCar.LoadAllRows(CurrentDatabase);

                var progressWindow = new ProgressWindow();
                progressWindow.Title = "Saving Parts Info";
                progressWindow.progressBar.Maximum = genericCar.Rows.Count;
                var progress = new Progress<(int Index, string CarLabel)>(prog =>
                {
                    progressWindow.lbl_progress.Content = $"{prog.Index} of {progressWindow.progressBar.Maximum}";
                    progressWindow.currentElement.Content = prog.CarLabel;
                    progressWindow.progressBar.Value = prog.Index;
                });

                var task = CurrentDatabase.SavePartsInfo(progressWindow, progress, dlg.FileName);
                progressWindow.ShowDialog();
                await task;
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

        private void ExportCurrentTable_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog("Select file to export the table as TXT");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.DefaultFileName = $"{CurrentTable.TableName}.txt";

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CurrentTable.ExportTableText(dlg.FileName);
            }
        }

        private void ExportCurrentTableCSV_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog("Select file to export the table as CSV");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.DefaultFileName = $"{CurrentTable.TableName}.csv";
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CurrentTable.ExportTableCSV(dlg.FileName);
            }
        }

        private async void lb_Tables_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (lb_Tables.SelectedIndex == -1)
                return;

            var table = CurrentDatabase.Tables[(string)lb_Tables.SelectedItem];

            if (!table.IsLoaded)
            {
                progressName.Text = $"Loading {table.TableName}..";
                progressBar.IsEnabled = true;
                progressBar.IsIndeterminate = true;
                try
                {
                    var loadTask = Task.Run(() =>
                    {
                        table.LoadAllRows(CurrentDatabase);
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

            CurrentTable = table;

            for (int i = cb_FilterColumnType.Items.Count - 1; i >= 2; i--)
                cb_FilterColumnType.Items.RemoveAt(i);

            PopulateTableColumns();
            SetNoProgress();

            dg_Rows.ItemsSource = CurrentTable.Rows;
            SetupFilters();
            cb_FilterColumnType.SelectedIndex = 1;
            mi_SaveTable.IsEnabled = true;
            mi_ExportTable.IsEnabled = true;
            mi_ExportTableCSV.IsEnabled = true;
            ToggleToolbarControls(true);
            
            statusName.Text = $"Loaded '{table.TableName}' with {CurrentTable.Rows.Count} rows.";
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
                    case DBColumnType.SByte:
                        newRow.ColumnData.Add(new DBSByte(0)); break;
                    case DBColumnType.Short:
                        newRow.ColumnData.Add(new DBShort(0)); break;
                    case DBColumnType.Int:
                        newRow.ColumnData.Add(new DBInt(0)); break;
                    case DBColumnType.UInt:
                        newRow.ColumnData.Add(new DBUInt(0)); break;
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
            if (!(e.EditingElement is TextBox tb))
                return;

            var currentRow = e.Row.Item as SpecDBRowData;
            string newInput = tb.Text;
            if (e.Column.Header.Equals("ID"))
            {
                if (int.TryParse(newInput, out int newValue))
                {
                    if (CurrentTable.Rows.Any(row => row.ID == newValue))
                    {
                        var res = MessageBox.Show("This ID is already being used by another row. Make sure you know what you are doing. Continue?", "ID in use", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res != MessageBoxResult.Yes)
                        {
                            currentRow.ID = CurrentTable.LastID + 1;
                            e.Cancel = true;
                            return;
                        }
                    }

                    var nextRow = CurrentTable.Rows.FirstOrDefault(r => r.ID >= newValue);
                    currentRow.ID = newValue;

                    // Put it to the last of said id if it conflicts
                    if (nextRow.ID == newValue)
                        nextRow = CurrentTable.Rows.FirstOrDefault(r => r.ID > newValue);

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
                    var res = MessageBox.Show("This Label is already being used by another row. Make sure you know what you are doing. Continue?", "Label in use", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes)
                    {
                        currentRow.Label = string.Empty;
                        e.Cancel = true;
                        return;
                    }
                }

                if (!newInput.All(c => char.IsLetterOrDigit(c) || c.Equals('_')))
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
                ColumnMetadata dataCol = CurrentTable.TableMetadata.Columns.Find(col => col.ColumnName == (string)e.Column.Header);
                if (dataCol.ColumnType == DBColumnType.Int)
                {
                    if (!int.TryParse(newInput, out int res))
                    {
                        MessageBox.Show($"Could not parse 'Integer' type. It must be number between {int.MinValue} and {int.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.UInt)
                {
                    if (!uint.TryParse(newInput, out uint res))
                    {
                        MessageBox.Show($"Could not parse 'Unsigned Integer' type. It must be number between {uint.MinValue} and {uint.MaxValue}.", "Validation Error",
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
                        MessageBox.Show($"Could not parse 'Unsigned Byte' type. It must be a number between {byte.MinValue} and {byte.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.SByte)
                {
                    if (!int.TryParse(newInput, out int res))
                    {
                        MessageBox.Show($"Could not parse 'Signed Byte' type. It must be a number between {sbyte.MinValue} and {sbyte.MaxValue}.", "Validation Error",
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

            ColumnMetadata dataCol = CurrentTable.TableMetadata.Columns.Find(col => col.ColumnName == (string)cell.Column.Header);
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
                    case DBColumnType.SByte:
                        newRow.ColumnData.Add(new DBSByte(((DBSByte)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.Short:
                        newRow.ColumnData.Add(new DBShort(((DBShort)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.Int:
                        newRow.ColumnData.Add(new DBInt(((DBInt)selectedRow.ColumnData[i]).Value)); break;
                    case DBColumnType.UInt:
                        newRow.ColumnData.Add(new DBUInt(((DBUInt)selectedRow.ColumnData[i]).Value)); break;
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

            var dlg = new SaveFileDialog();
            if (dlg.ShowDialog() == true)
            {
                var table = CurrentDatabase.Tables[(string)lb_Tables.SelectedItem];
                int rows = table.DumpTable(dlg.FileName);
                MessageBox.Show($"Dumped table with {rows} rows at {dlg.FileName}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void tb_ColumnFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb_ColumnFilter.Text != null && tb_ColumnFilter.Text.Length != 1)
                FilterString = tb_ColumnFilter.Text;
        }

        private void dg_Rows_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Clipboard.ContainsText())
            {
                string clipText = Clipboard.GetText();
                string[] textSpl = clipText.Split('\t');
                if (textSpl.Length != 2 + CurrentTable.TableMetadata.Columns.Count)
                {
                    e.Handled = true;
                    return;
                }

                // Verify ID and Label first
                if (!int.TryParse(textSpl[0], out int id))
                {
                    e.Handled = true;
                    return;
                }

                if (CurrentTable.Rows.FirstOrDefault(row => row.ID == id || (row.Label != null && row.Label.Equals(textSpl[1]))) != null)
                {
                    var res = MessageBox.Show("The pasted row has an ID or Label that is already being used by another row. Make sure you know what you are doing. Continue?", "ID/Label in use", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes)
                    {
                        e.Handled = true;
                        return;
                    }
                }

                var dbRow = dg_Rows.SelectedItem as SpecDBRowData;
                dbRow.ID = id;

                // Reorder
                var nextRow = CurrentTable.Rows.FirstOrDefault(r => r.ID >= id);

                // Put it to the last of said id if it conflicts
                if (nextRow.ID == id)
                    nextRow = CurrentTable.Rows.FirstOrDefault(r => r.ID > id);

                if (nextRow is null) // End of list?
                    CurrentTable.Rows.Move(CurrentTable.Rows.IndexOf(dbRow), CurrentTable.Rows.Count - 1);
                else
                {
                    var nextRowIndex = CurrentTable.Rows.IndexOf(nextRow);
                    if (nextRowIndex > CurrentTable.Rows.IndexOf(dbRow)) // If the row is being moved backwards
                        nextRowIndex--;

                    CurrentTable.Rows.Move(CurrentTable.Rows.IndexOf(dbRow), nextRowIndex);
                }

                dbRow.Label = textSpl[1];

                textSpl[textSpl.Length - 1].TrimEnd();
                for (int i = 2; i < textSpl.Length; i++)
                {
                    IDBType colData = dbRow.ColumnData[i - 2];
                    switch (colData)
                    {
                        case DBByte @byte:
                            if (byte.TryParse(textSpl[i], out byte vByte)) @byte.Value = vByte;
                            break;
                        case DBSByte @sbyte:
                            if (sbyte.TryParse(textSpl[i], out sbyte vSbyte)) @sbyte.Value = vSbyte;
                            break;
                        case DBFloat @float:
                            if (float.TryParse(textSpl[i], out float vFloat)) @float.Value = vFloat;
                            break;
                        case DBInt @int:
                            if (int.TryParse(textSpl[i], out int vInt)) @int.Value = vInt;
                            break;
                        case DBUInt @uint:
                            if (uint.TryParse(textSpl[i], out uint vUInt)) @uint.Value = vUInt;
                            break;
                        case DBLong @long:
                            if (long.TryParse(textSpl[i], out long vLong)) @long.Value = vLong;
                            break;
                        case DBShort @short:
                            if (short.TryParse(textSpl[i], out short vShort)) @short.Value = vShort;
                            break;
                        case DBBool @bool:
                            if (bool.TryParse(textSpl[i], out bool vBool)) @bool.Value = vBool;
                            break;
                        case DBString @str:
                            var strDb = CurrentDatabase.StringDatabases[@str.FileName];
                            @str.StringIndex = strDb.GetOrCreate(textSpl[i]);
                            @str.Value = textSpl[i];
                            break;

                    }
                }
            }
            else if (e.Key == Key.Delete)
            {

            }

        }

        public void SetupFilters()
        {
            foreach (var col in CurrentTable.TableMetadata.Columns)
                cb_FilterColumnType.Items.Add(col.ColumnName);

            _dataGridCollection = CollectionViewSource.GetDefaultView(dg_Rows.ItemsSource);
            if (_dataGridCollection != null)
                _dataGridCollection.Filter = FilterTask;
                
        }

        public bool FilterTask(object value)
        {
            if (string.IsNullOrEmpty(FilterString) || FilterString.Length < 2)
                return true;

            if (value is SpecDBRowData row && row.ColumnData.Count != 0)
            {

                if (cb_FilterColumnType.SelectedIndex == 0)
                    return row.ID.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                else if (cb_FilterColumnType.SelectedIndex == 1)
                    return row.Label.Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                else
                {
                    var colData = row.ColumnData[cb_FilterColumnType.SelectedIndex - 2];
                    switch (colData)
                    {
                        case DBByte @byte:
                            return @byte.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                        case DBSByte @sbyte:
                            return @sbyte.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                        case DBFloat @float:
                            return @float.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                        case DBInt @int:
                            return @int.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                        case DBUInt @uint:
                            return @uint.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                        case DBLong @long:
                            return @long.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                        case DBShort @short:
                            return @short.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                        case DBString @str:
                            return @str.Value.Contains(FilterString, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return false;
        }

        private void PopulateTableColumns()
        {
            dg_Rows.ItemsSource = null;
            for (int i = dg_Rows.Columns.Count - 1; i >= 2; i--)
                dg_Rows.Columns.Remove(dg_Rows.Columns[i]);

            for (int i = 0; i < CurrentTable.TableMetadata.Columns.Count; i++)
            {
                ColumnMetadata column = CurrentTable.TableMetadata.Columns[i];
                var style = new Style(typeof(DataGridColumnHeader));
                style.Setters.Add(new Setter(ToolTipService.ToolTipProperty, $"Type: {column.ColumnType}"));

                if (column.ColumnType == DBColumnType.Bool)
                {
                    dg_Rows.Columns.Add(new DataGridCheckBoxColumn
                    {
                        HeaderStyle = style,
                        Header = column.ColumnName,
                        Binding = new Binding($"ColumnData[{i}].Value"),
                    });
                }
                else
                {
                    dg_Rows.Columns.Add(new DataGridTextColumn
                    {
                        HeaderStyle = style,
                        Header = column.ColumnName,
                        Binding = new Binding($"ColumnData[{i}].Value"),
                        IsReadOnly = column.ColumnType == DBColumnType.String,
                    });
                }
            }
        }

        private void SetNoProgress()
        {
            progressName.Text = "Ready";
            progressBar.IsEnabled = false;
            progressBar.IsIndeterminate = false;
        }

        private void ToggleToolbarControls(bool enabled)
        {
            cb_FilterColumnType.IsEnabled = enabled;
            btn_AddRow.IsEnabled = enabled;
            btn_DeleteRow.IsEnabled = enabled;
            btn_CopyRow.IsEnabled = enabled;
            tb_ColumnFilter.IsEnabled = enabled;
        }
    }
}
