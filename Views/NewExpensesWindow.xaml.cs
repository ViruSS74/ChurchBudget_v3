using ChurchBudget_v3_WPF.Models;
using ChurchBudget_v3_WPF.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static ChurchBudget_v3_WPF.Services.ExpensesCategoryService;

namespace ChurchBudget_v3_WPF.Views
    {
        public partial class NewExpensesWindows : Window
        {
            private readonly ExpensesCategoryService _categoryService;
            private readonly ListOfDocsService _listOfDocsService;
            private readonly string _connectionString;
            private readonly string _receiptsDir;
            private ObservableCollection<DocExpenseItemUI> _docItems;

            public NewExpensesWindows()
            {
                InitializeComponent();
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data\church.db");
                _connectionString = string.Format("Data Source={0};Version=3;", dbPath);
                _categoryService = new ExpensesCategoryService();
                _listOfDocsService = new ListOfDocsService(dbPath);
                _receiptsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Receipts");
                if (!Directory.Exists(_receiptsDir)) Directory.CreateDirectory(_receiptsDir);

                _docItems = new ObservableCollection<DocExpenseItemUI>();
                dgDocumentItems.ItemsSource = _docItems;
                _docItems.CollectionChanged += (s, e) => UpdateTotalAmount();
                this.Loaded += NewExpensesWindows_Loaded;
                dpDocDate.SelectedDateChanged += DpDocDate_SelectedDateChanged;
            }

            private void NewExpensesWindows_Loaded(object sender, RoutedEventArgs e)
            {
                dpDocDate.SelectedDate = DateTime.Today;
                LoadCategoryTree();
                LoadPersonalCombo(); // <- Добавили сюда!
            }

        private void DpDocDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
            {
                if (dpDocDate.SelectedDate.HasValue && _listOfDocsService != null)
                {
                    DateTime date = dpDocDate.SelectedDate.Value;
                    txtDocNumber.Text = _listOfDocsService.GetNextDocNumber(date, "Р");
                }
            }

            private void dgCategories_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                if (sender is TreeViewItem treeViewItem && !treeViewItem.IsSelected) return;
                var selectedCat = tvCategories.SelectedItem as ExpensesCategoryUI;
                if (selectedCat == null) return;
                if (selectedCat.Children != null && selectedCat.Children.Count > 0) return;

                e.Handled = true;

                if (_docItems.Any(x => x.CategoryId == selectedCat.id))
                {
                    MessageBox.Show("Эта категория уже добавлена в документ!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var newItem = new DocExpenseItemUI
                {
                    CategoryId = selectedCat.id,
                    CategoryName = selectedCat.name,
                    Amount = 0m,
                    CheckNumber = string.Empty,
                    Basis = string.Empty,
                    ReceiptPath = string.Empty
                };

                newItem.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(DocExpenseItemUI.Amount)) UpdateTotalAmount();
                };
                _docItems.Add(newItem);
            }

            private void UpdateTotalAmount()
            {
                decimal total = _docItems.Sum(item => item.Amount);
                txtTotalAmount.Text = total.ToString("N2");
            }

            private void dgDocumentItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateTotalAmount()), System.Windows.Threading.DispatcherPriority.Background);
            }

            private void CloseButton_Click(object sender, RoutedEventArgs e)
            {
                this.DialogResult = false;
                this.Close();
            }

            private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton == MouseButton.Left) this.DragMove();
            }

            private void txtAmount_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
            {
                if (e.Text == "." || e.Text == ",")
                {
                    TextBox textBox = sender as TextBox;
                    if (textBox != null && !textBox.Text.Contains(",") && !textBox.Text.Contains("."))
                    {
                        int caretIndex = textBox.CaretIndex;
                        textBox.Text = textBox.Text.Insert(caretIndex, ",");
                        textBox.CaretIndex = caretIndex + 1;
                    }
                    e.Handled = true;
                }
            }

            private void txtAmount_GotFocus(object sender, RoutedEventArgs e)
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null) textBox.SelectAll();
            }

            private void dgDocumentItems_PreparingCellForEdit(object sender, System.Windows.Controls.DataGridPreparingCellForEditEventArgs e)
            {
                var textBox = e.EditingElement as TextBox;
                if (textBox != null)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                    textBox.PreviewTextInput -= TableTextBox_PreviewTextInput;
                    textBox.PreviewTextInput += TableTextBox_PreviewTextInput;
                }
            }

            private void TableTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
            {
                if (e.Text == "." || e.Text == ",")
                {
                    var textBox = sender as TextBox;
                    if (textBox != null && !textBox.Text.Contains(","))
                    {
                        int caretIndex = textBox.CaretIndex;
                        textBox.Text = textBox.Text.Insert(caretIndex, ",");
                        textBox.CaretIndex = caretIndex + 1;
                    }
                    e.Handled = true;
                }
            }

        // КНОПКА СОХРАНИТЬ РАСХОД + РКО (С новыми полями ФИО, Основание и РКО)
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDocNumber.Text)) return;
            if (_docItems.Count == 0) return;
            if (dpDocDate.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату документа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = new SQLiteConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    if (_listOfDocsService.IsNumberExists(txtDocNumber.Text, "expense_docs", conn))
                    {
                        MessageBox.Show("Документ с таким номером уже существует!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            decimal currentTotalSum = _docItems.Sum(item => item.Amount);

                            // A. Вставка в expense_docs (Включая новые поля person_name и main_basis)
                            string sqlDoc = @"INSERT INTO expense_docs (doc_number, date, total, person_name, main_basis) 
                                      VALUES (@n, @d, @t, @pName, @mBasis); 
                                      SELECT last_insert_rowid();";
                            long docId;
                            using (var cmdDoc = new SQLiteCommand(sqlDoc, conn, trans))
                            {
                                cmdDoc.Parameters.AddWithValue("@n", txtDocNumber.Text.Trim());
                                cmdDoc.Parameters.AddWithValue("@d", dpDocDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmdDoc.Parameters.AddWithValue("@t", (double)currentTotalSum);
                                cmdDoc.Parameters.AddWithValue("@pName", cmbMainPerson.Text.Trim());
                                cmdDoc.Parameters.AddWithValue("@mBasis", txtMainBasis.Text.Trim());
                                docId = Convert.ToInt64(cmdDoc.ExecuteScalar());
                            }

                            // Явно прописываем системные списки для чеков и оснований
                            System.Collections.Generic.List<string> basisList = new System.Collections.Generic.List<string>();
                            System.Collections.Generic.List<string> checkList = new System.Collections.Generic.List<string>();

                            // Б. Вставка строк в expense_items
                            foreach (var item in _docItems)
                            {
                                if (!string.IsNullOrEmpty(item.Basis) && !basisList.Contains(item.Basis)) basisList.Add(item.Basis);
                                if (!string.IsNullOrEmpty(item.CheckNumber) && !checkList.Contains(item.CheckNumber)) checkList.Add(item.CheckNumber);

                                string sqlItem = @"INSERT INTO expense_items (doc_id, category, amount, description, check_number, basis) 
                                           VALUES (@id, @c, @a, @desc, @check, @basis)";
                                using (var cmdItem = new SQLiteCommand(sqlItem, conn, trans))
                                {
                                    cmdItem.Parameters.AddWithValue("@id", docId);
                                    cmdItem.Parameters.AddWithValue("@c", item.CategoryName);
                                    cmdItem.Parameters.AddWithValue("@a", (double)item.Amount);
                                    cmdItem.Parameters.AddWithValue("@desc", item.ReceiptPath ?? "");
                                    cmdItem.Parameters.AddWithValue("@check", item.CheckNumber ?? "");
                                    cmdItem.Parameters.AddWithValue("@basis", item.Basis ?? "");
                                    cmdItem.ExecuteNonQuery();
                                }
                            }

                            // Формируем основание для РКО
                            string finalBasis = txtMainBasis.Text.Trim();
                            if (string.IsNullOrEmpty(finalBasis))
                            {
                                // Если общее поле пустое, собираем по старой логике из ячеек таблицы
                                finalBasis = string.Join("; ", basisList);
                                if (string.IsNullOrEmpty(finalBasis)) finalBasis = "Хозяйственные расходы по документу №" + txtDocNumber.Text.Trim();
                            }

                            string allChecks = string.Join("; ", checkList);

                            // В. Создание РКО на основе новых параметров из шапки ведомости
                            string rkoNumber = txtDocNumber.Text.Trim().Replace("Р-", "РКО-");
                            string sqlRKO = @"INSERT INTO cash_orders (order_type, order_number, doc_ref_id, date, amount, base, appendix, person_name_manual)
                                      VALUES ('РКО', @rNum, @refId, @date, @amt, @base, @app, @pName)";
                            using (var cmdRko = new SQLiteCommand(sqlRKO, conn, trans))
                            {
                                cmdRko.Parameters.AddWithValue("@rNum", rkoNumber);
                                cmdRko.Parameters.AddWithValue("@refId", docId);
                                cmdRko.Parameters.AddWithValue("@date", dpDocDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmdRko.Parameters.AddWithValue("@amt", (double)currentTotalSum);
                                cmdRko.Parameters.AddWithValue("@base", finalBasis);
                                cmdRko.Parameters.AddWithValue("@app", string.IsNullOrEmpty(allChecks) ? (object)DBNull.Value : allChecks);
                                cmdRko.Parameters.AddWithValue("@pName", string.IsNullOrWhiteSpace(cmbMainPerson.Text) ? (object)DBNull.Value : cmbMainPerson.Text.Trim());
                                cmdRko.ExecuteNonQuery();
                            }

                            trans.Commit();
                            MessageBox.Show($"Документ №{txtDocNumber.Text} и РКО №{rkoNumber} успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            ResetFormForNewDocument();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show("Ошибка сохранения транзакции расходов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка работы с базой данных SQLite (Расходы): " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetFormForNewDocument()
        {
            _docItems.Clear();
            UpdateTotalAmount();

            // ИСПРАВЛЕНИЕ ДЛЯ КОМБОБОКСА РАСХОДОВ: Сбрасываем выбор из списка сотрудников и очищаем поле
            cmbMainPerson.SelectedIndex = -1;
            cmbMainPerson.Text = string.Empty;

            txtMainBasis.Text = string.Empty;

            DateTime selectedDate = dpDocDate.SelectedDate ?? DateTime.Now;
            dpDocDate.SelectedDate = selectedDate;
            try
            {
                // Передаем префикс "Р" для расходных ведомостей
                txtDocNumber.Text = _listOfDocsService.GetNextDocNumber(selectedDate, "Р");
            }
            catch
            {
                // Резервный вариант при сбое генератора номеров
                string datePart = selectedDate.ToString("ddMMyyyy");
                txtDocNumber.Text = $"Р-{datePart}-001";
            }

            txtDocNumber.Focus();
            if (!string.IsNullOrEmpty(txtDocNumber.Text)) txtDocNumber.SelectAll();
        }

        private void LoadCategoryTree()
        {
            // Указываем компилятору точный путь к List
            System.Collections.Generic.List<ExpensesCategoryItem> dbCategories = _categoryService.GetAll();
            if (dbCategories == null) return;

            System.Collections.Generic.List<ExpensesCategoryUI> uiList = dbCategories.Select(item => new ExpensesCategoryUI { DbItem = item }).ToList();

            foreach (var cat in uiList)
            {
                cat.Children = new System.Collections.Generic.List<ExpensesCategoryUI>();
            }

            var categoryMap = uiList.ToDictionary(c => c.id);

            foreach (var category in uiList)
            {
                if (category.parent_id.HasValue && category.parent_id.Value != 0)
                {
                    if (categoryMap.TryGetValue(category.parent_id.Value, out var parent))
                        parent.Children.Add(category);
                }
            }

            var rootCategories = uiList.Where(c => c.parent_id == null || c.parent_id == 0).ToList();
            tvCategories.ItemsSource = rootCategories;
        }


        private void LoadPersonalCombo()
        {
            // Заменяем var на полный путь к коллекции
            System.Collections.Generic.List<PersonalItem> staffList = new System.Collections.Generic.List<PersonalItem>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT id, last_name, first_name, middle_name FROM personal ORDER BY last_name ASC;";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            staffList.Add(new PersonalItem
                            {
                                Id = Convert.ToInt64(reader["id"]),
                                LastName = reader["last_name"]?.ToString(),
                                FirstName = reader["first_name"]?.ToString(),
                                MiddleName = reader["middle_name"]?.ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки списка сотрудников: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            cmbMainPerson.ItemsSource = staffList;
            cmbMainPerson.DisplayMemberPath = "FullName";
            cmbMainPerson.SelectedValuePath = "FullName";
        }
    }
}