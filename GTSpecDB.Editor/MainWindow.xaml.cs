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

using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;

using Humanizer;

using PDTools.SpecDB.Core;
using PDTools.SpecDB.Core.Mapping;
using PDTools.SpecDB.Core.Mapping.Types;
using PDTools.Utils;

namespace GTSpecDB.Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public const string WindowTitle = "Gran Turismo Spec Database Editor";

        public SpecDB CurrentDatabase { get; set; }
        public PDTools.SpecDB.Core.Table CurrentTable { get; set; }
        public string SpecDBDirectory { get; set; }

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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 1)
                        return;

                    if (!Directory.Exists(files[0]))
                        return;

                    var specType = SpecDB.DetectSpecDBType(Path.GetFileName(files[0]));
                    if (specType is null)
                    {
                        var window = new SpecDBKindSelector();
                        window.ShowDialog();
                        if (!window.HasSelected || window.SelectedType == SpecDBFolder.NONE)
                            return;

                        specType = window.SelectedType;
                    }

                    CurrentDatabase?.Dispose();

                    CurrentDatabase = SpecDB.LoadFromSpecDBFolder(files[0], specType.Value, false);
                    SpecDBDirectory = files[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load SpecDB: {ex.Message}", "Failed to load the SpecDB", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ProcessNewlyLoadedSpecDB();
        }

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

        #region Top Menu
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
                    var specType = SpecDB.DetectSpecDBType(Path.GetFileName(dlg.FileName));
                    if (specType is null)
                    {
                        var window = new SpecDBKindSelector();
                        window.ShowDialog();
                        if (!window.HasSelected || window.SelectedType == SpecDBFolder.NONE)
                            return;
                        specType = window.SelectedType;
                    }

                    CurrentDatabase?.Dispose();

                    CurrentDatabase = SpecDB.LoadFromSpecDBFolder(dlg.FileName, specType.Value, false);
                    SpecDBDirectory = dlg.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not load SpecDB: {ex.Message}", "Failed to load the SpecDB", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ProcessNewlyLoadedSpecDB();
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
                if (!CurrentDatabase.Tables.TryGetValue("GENERIC_CAR", out PDTools.SpecDB.Core.Table genericCar))
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

                var task = SavePartsInfoFileAsync(progressWindow, progress, true, dlg.FileName);
                progressWindow.ShowDialog();
                await task;

                statusName.Text = $"PartsInfo.tbi/tbd saved to {dlg.FileName}.";
            }
        }

        async Task SavePartsInfoFileAsync(ProgressWindow progressWindow, Progress<(int, string)> progress, bool tbdFile, string fileName)
        {
            try
            {
                await Task.Run(() => CurrentDatabase.SavePartsInfoFile(progress, tbdFile, fileName));
            }
            finally
            {
                progressWindow.Close();
            }
        }

        /// <summary>
        /// Parts Table saving
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveCarsParts_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog("Select folder to save the CARS folder");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.IsFolderPicker = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (!CurrentDatabase.Tables.TryGetValue("GENERIC_CAR", out PDTools.SpecDB.Core.Table genericCar))
                {
                    MessageBox.Show($"Can not save the CARS folder as GENERIC_CAR is missing.", "Table not loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                var task = SavePartsInfoFileAsync(progressWindow, progress, false, dlg.FileName);
                progressWindow.ShowDialog();
                await task;

                statusName.Text = $"Car parts saved to {dlg.FileName}.";
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
                statusName.Text = $"Table saved to {dlg.FileName}.";
            }
        }

        private void ExportCurrentTable_Click(object sender, RoutedEventArgs e)
        {
            CommonSaveFileDialog dlg = new CommonSaveFileDialog("Select file to export the table as TXT");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.DefaultFileName = $"{CurrentTable.TableName}.txt";

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                CurrentTable.ExportTableText(((ShellFile)dlg.FileAsShellObject).Path);
        }

        private void ExportCurrentTableCSV_Click(object sender, RoutedEventArgs e)
        {
            CommonSaveFileDialog dlg = new CommonSaveFileDialog("Select file to export the table as CSV");
            dlg.EnsurePathExists = true;
            dlg.EnsureFileExists = true;
            dlg.DefaultFileName = $"{CurrentTable.TableName}.csv";
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                CurrentTable.ExportTableCSV(((ShellFile)dlg.FileAsShellObject).Path);
        }

        private async void ExportCurrentTableSQLite_Click(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion

        #region Toolbar
        private void btn_AddRow_Click(object sender, RoutedEventArgs e)
        {
            var newRow = new RowData();
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
                    case DBColumnType.UShort:
                        newRow.ColumnData.Add(new DBUShort(0)); break;
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

            dg_Rows.ScrollIntoView(newRow);
        }

        private void btn_DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Rows.SelectedIndex == -1 || !dg_Rows.CurrentCell.IsValid)
                return;

            CurrentTable.Rows.Remove(dg_Rows.CurrentCell.Item as RowData);
            CurrentTable.LastID = CurrentTable.Rows.Max(row => row.ID);

            statusName.Text = "Row deleted.";
        }

        private void btn_CopyRow_Click(object sender, RoutedEventArgs e)
        {
            if (dg_Rows.SelectedIndex == -1 || !dg_Rows.CurrentCell.IsValid)
                return;

            var selectedRow = dg_Rows.CurrentCell.Item as RowData;

            var newRow = new RowData();
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
                    case DBColumnType.UShort:
                        newRow.ColumnData.Add(new DBUShort(((DBUShort)selectedRow.ColumnData[i]).Value)); break;
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

            dg_Rows.ScrollIntoView(newRow);
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(CurrentTable.TableName);
            CurrentTable.SaveTable(CurrentDatabase, SpecDBDirectory);
            statusName.Text = $"Table saved to {SpecDBDirectory}.";
        }

        private void btn_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentTable_Click(sender, e);
        }

        private void tb_ColumnFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb_ColumnFilter.Text != null && (tb_ColumnFilter.Text.Length > 3 || tb_ColumnFilter.Text.Length == 0))
            {
                // Apparently only twice works, so lol
                dg_Rows.CancelEdit();
                dg_Rows.CancelEdit();
                FilterString = tb_ColumnFilter.Text;
            }
        }
        #endregion

        #region Datagrid Events
        private void dg_Rows_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!(e.EditingElement is TextBox tb))
                return;

            var currentRow = e.Row.Item as RowData;
            string newInput = tb.Text;
            if (dg_Rows.Columns[0] == e.Column) // Editing ID column
            {
                if (int.TryParse(newInput, out int newValue))
                {
                    if (CurrentTable.Rows.Any(row => row.ID == newValue && row != currentRow))
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
                    if (nextRow != null && nextRow.ID == newValue)
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
                    dg_Rows.ScrollIntoView(currentRow);
                }
                else
                {
                    currentRow.ID = CurrentTable.LastID + 1;
                    e.Cancel = true;
                }
            }
            else if (dg_Rows.Columns[1] == e.Column) // Editing Label Column
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
                ColumnMetadata dataCol = CurrentTable.TableMetadata.Columns[dg_Rows.Columns.IndexOf(e.Column) - 2];
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
                    if (!short.TryParse(newInput, out short res))
                    {
                        MessageBox.Show($"Could not parse 'Short' type. It must be number between {short.MinValue} and {short.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.Short)
                {
                    if (!ushort.TryParse(newInput, out ushort res))
                    {
                        MessageBox.Show($"Could not parse 'UShort' type. It must be number between {ushort.MinValue} and {ushort.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.Byte)
                {
                    if (!byte.TryParse(newInput, out byte res))
                    {
                        MessageBox.Show($"Could not parse 'Unsigned Byte' type. It must be a number between {byte.MinValue} and {byte.MaxValue}.", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Cancel = true;
                    }
                }
                else if (dataCol.ColumnType == DBColumnType.SByte)
                {
                    if (!sbyte.TryParse(newInput, out sbyte res))
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

            var colIndex = dg_Rows.Columns.IndexOf(cell.Column);
            if (colIndex == 0 || colIndex == 1)
                return;

            ColumnMetadata dataCol = CurrentTable.TableMetadata.Columns[colIndex - 2];
            if (dataCol is null)
                return;

            if (dataCol.ColumnType == DBColumnType.String)
            {
                var strDb = CurrentDatabase.StringDatabases[dataCol.StringFileName];

                // Find column index to apply our row data to
                var row = cell.DataContext as RowData;
                int columnIndex = CurrentTable.TableMetadata.Columns.IndexOf(dataCol);

                var str = row.ColumnData[columnIndex] as DBString;
                int index = strDb.Strings.IndexOf(str.Value);

                var strWindow = new StringDatabaseManager(strDb, index);
                strWindow.ShowDialog();
                if (strWindow.HasSelected)
                {
                    // Apply string change
                    str.StringIndex = strWindow.SelectedString.index;
                    str.Value = strWindow.SelectedString.selectedString;
                }
            }
        }

        private void dg_Rows_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Clipboard.ContainsText())
            {
                string clipText = Clipboard.GetText();
                string[] textSpl = clipText.Split('\t');
                if (textSpl.Length > 2 + CurrentTable.TableMetadata.Columns.Count)
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

                var dbRow = dg_Rows.SelectedItem as RowData;
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

                dbRow.Label = textSpl[1].TrimEnd();

                textSpl[textSpl.Length - 1].TrimEnd();
                for (int i = 2; i < textSpl.Length; i++)
                {
                    IDBType colData = dbRow.ColumnData[i - 2];
                    textSpl[i] = textSpl[i].TrimEnd();
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
                        case DBUShort @ushort:
                            if (ushort.TryParse(textSpl[i], out ushort vUShort)) @ushort.Value = vUShort;
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

        private void dg_Rows_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            dg_cm_CopyCell.IsEnabled = lb_Tables.SelectedIndex != -1 && dg_Rows.SelectedIndex != -1;
            dg_cm_ViewRaceEntries.IsEnabled = lb_Tables.SelectedIndex != -1 && dg_Rows.SelectedIndex != -1 && CurrentTable.TableName == "RACE";
        }

        private void dg_Rows_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!dg_Rows.CurrentCell.IsValid)
                return;

            var row = dg_Rows.CurrentCell.Item as RowData;

            tb_CurrentId.Text = row.ID.ToString();
            tb_CurrentLabel.Text = row.Label;
        }

        private void dg_cm_CopyCell_Click(object sender, RoutedEventArgs e)
        {
            if (!dg_Rows.CurrentCell.IsValid)
                return;

            var colIndex = dg_Rows.Columns.IndexOf(dg_Rows.CurrentCell.Column);
            var row = dg_Rows.CurrentCell.Item as RowData;

            string output;
            if (colIndex == 0)
                output = row.ID.ToString();
            else if (colIndex == 1)
                output = row.Label;
            else
            {
                var columnData = row.ColumnData[colIndex - 2];
                if (columnData is DBString strData)
                    output = CurrentDatabase.StringDatabases[strData.FileName].Strings[strData.StringIndex];
                else
                    output = row.ColumnData[colIndex].ToString();

                if (string.IsNullOrEmpty(output))
                    output = "";
                Clipboard.SetText(output);
            }

            if (string.IsNullOrEmpty(output))
                output = "";
            Clipboard.SetText(output);

            statusName.Text = $"Copied cell '{output}'";
        }

        private void dg_cm_ViewRaceEntries_Click(object sender, RoutedEventArgs e)
        {
            int tableId = CurrentTable.TableID;

            ;
        }
        #endregion

        #region Table Listing
        private async void lb_Tables_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (lb_Tables.SelectedIndex == -1)
                return;

            // Ensure to cancel the edit to properly allow filtering reset
            dg_Rows.CancelEdit();
            dg_Rows.CancelEdit();

            var table = CurrentDatabase.Tables[(string)lb_Tables.SelectedItem];

            if (!table.IsLoaded)
            {
                statusName.Text = $"Loading {table.TableName}..";
                progressBar.IsEnabled = true;
                progressBar.IsIndeterminate = true;
                try
                {
                    var loadTask = Task.Run(() => table.LoadAllRows(CurrentDatabase));
                    await loadTask;

                    if (!table.IsTableProperlyMapped)
                        MessageBox.Show($"This table is not entirely mapped - display errors & missing data may be present.", "Table not mapped correctly", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            mi_SaveTable.IsEnabled = true;
            mi_ExportTable.IsEnabled = true;
            mi_ExportTableCSV.IsEnabled = true;

            ToggleToolbarControls(true);

            statusName.Text = $"Loaded '{table.TableName}' with {CurrentTable.Rows.Count} rows.";
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

        private void cm_DumpDebugTable_Click(object sender, RoutedEventArgs e)
        {
            if (lb_Tables.SelectedIndex == -1)
                return;

            var debug = new SpecDBDebugPrinter();
            debug.Load(Path.Combine(CurrentDatabase.FolderName, CurrentTable.TableName) + ".dbt");
            debug.Print();
        }

        private void lb_Tables_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (lb_Tables.SelectedIndex == -1)
                return;

            var table = CurrentDatabase.Tables[(string)lb_Tables.SelectedItem];
            cm_TableIndex.Header = $"Table Index: {table.IDI.TableIndex}";
        }
        #endregion

        #region Non-Events
        public void SetupFilters()
        {
            foreach (var col in CurrentTable.TableMetadata.Columns)
                cb_FilterColumnType.Items.Add(col.ColumnName);

            if (cb_FilterColumnType.SelectedIndex == -1)
            {
                tb_ColumnFilter.Text = _filterString = string.Empty;
                cb_FilterColumnType.SelectedIndex = 1; // Reset to label
            }

            _dataGridCollection = CollectionViewSource.GetDefaultView(dg_Rows.ItemsSource);
            if (_dataGridCollection != null)
                _dataGridCollection.Filter = FilterTask;
                
        }

        public bool FilterTask(object value)
        {
            if (string.IsNullOrEmpty(FilterString) || FilterString.Length < 3)
                return true;

            if (value is RowData row && row.ColumnData.Count != 0)
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
                        case DBUShort @ushort:
                            return @ushort.Value.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase);
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
            statusName.Text = "Ready";
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

            btn_SaveAs.IsEnabled = enabled;
            btn_Save.IsEnabled = enabled;
        }

        private void ProcessNewlyLoadedSpecDB()
        {
            CurrentTable = null;
            dg_Rows.ItemsSource = null;
            tb_ColumnFilter.Text = "";
            FilterString = "";

            mi_SavePartsInfo.IsEnabled = CurrentDatabase.SpecDBFolderType >= SpecDBFolder.GT5_JP3009;
            mi_SaveCarsParts.IsEnabled = CurrentDatabase.SpecDBFolderType <= SpecDBFolder.GT5_TRIAL_JP2704;

            mi_ExportTableSQLite.IsEnabled = true;
            lb_Tables.Items.Clear();

            foreach (var table in CurrentDatabase.Tables)
                lb_Tables.Items.Add($"{table.Key}");

            this.Title = $"{WindowTitle} - {CurrentDatabase.SpecDBFolderType} [{CurrentDatabase.SpecDBFolderType.Humanize()}]";

            ToggleToolbarControls(false);
        }
        #endregion


    }
}
