using ChurchBudget_v3_WPF.Models;
using ChurchBudget_v3_WPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static ChurchBudget_v3_WPF.Services.IncomeCategoryService;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class NewIncomeWindow : Window
    {
        private readonly IncomeCategoryService _categoryService;
        private readonly ListOfDocsService _listOfDocsService;
        private readonly string _connectionString;
        private ObservableCollection<DocItemUI> _docItems;

        public NewIncomeWindow()
        {
            InitializeComponent();
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data\church.db");
            _connectionString = string.Format("Data Source={0};Version=3;", dbPath);
            _categoryService = new IncomeCategoryService();
            _listOfDocsService = new ListOfDocsService(dbPath);
            _docItems = new ObservableCollection<DocItemUI>();
            dgDocumentItems.ItemsSource = _docItems;
            _docItems.CollectionChanged += (s, e) => UpdateTotalAmount();
            this.Loaded += NewIncomeWindows_Loaded;
            dpDocDate.SelectedDateChanged += DpDocDate_SelectedDateChanged;
        }

        private void NewIncomeWindows_Loaded(object sender, RoutedEventArgs e)
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
                txtDocNumber.Text = _listOfDocsService.GetNextDocNumber(date, "Д");
            }
        }

        private void dgCategories_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem && !treeViewItem.IsSelected) return;
            var selectedCat = tvCategories.SelectedItem as IncomeCategoryUI;
            if (selectedCat == null) return;
            if (selectedCat.Children != null && selectedCat.Children.Count > 0) return;

            e.Handled = true;

            if (_docItems.Any(x => x.CategoryId == selectedCat.id))
            {
                MessageBox.Show("Эта категория дохода уже добавлена в документ!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var newItem = new DocItemUI
            {
                CategoryId = selectedCat.id,
                CategoryName = selectedCat.name,
                Amount = 0m,
                Comment = string.Empty
            };

            newItem.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(DocItemUI.Amount)) UpdateTotalAmount();
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

        // КНОПКА "СОХРАНИТЬ" (С новыми полями ФИО, Основание и ПКО)
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDocNumber.Text))
            {
                MessageBox.Show("Номер документа не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_docItems.Count == 0)
            {
                MessageBox.Show("Таблица пуста! Добавьте хотя бы одну позицию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = new SQLiteConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    if (_listOfDocsService.IsNumberExists(txtDocNumber.Text, "income_docs", conn))
                    {
                        MessageBox.Show("Документ № " + txtDocNumber.Text + " уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            decimal currentTotalSum = _docItems.Sum(item => item.Amount);

                            // А. Сохраняем заголовок документа (Включая новые поля person_name и main_basis)
                            string sqlDoc = @"INSERT INTO income_docs (doc_number, date, total, person_name, main_basis) 
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

                            // Б. Сохраняем строки в цикле (income_items)
                            foreach (var item in _docItems)
                            {
                                string sqlItem = "INSERT INTO income_items (doc_id, category, amount, description) VALUES (@id, @c, @a, @desc)";
                                using (var cmdItem = new SQLiteCommand(sqlItem, conn, trans))
                                {
                                    cmdItem.Parameters.AddWithValue("@id", docId);
                                    cmdItem.Parameters.AddWithValue("@c", item.CategoryName);
                                    cmdItem.Parameters.AddWithValue("@a", (double)item.Amount);
                                    cmdItem.Parameters.AddWithValue("@desc", item.Comment ?? "");
                                    cmdItem.ExecuteNonQuery();
                                }
                            }

                            // В. Автоматическое создание ПКО на основе данных из шапки ведомости
                            string pkoNumber = txtDocNumber.Text.Trim().Replace("Д-", "ПКО-");

                            // Готовим основание для ПКО: если текстовое поле пустое, берем авто-вариант
                            string finalBasis = !string.IsNullOrWhiteSpace(txtMainBasis.Text)
                                ? txtMainBasis.Text.Trim()
                                : $"Приход по приходному документу №{txtDocNumber.Text.Trim()}";

                            string sqlPKO = @"INSERT INTO cash_orders (order_type, order_number, doc_ref_id, date, amount, base, person_name_manual) 
                                              VALUES ('ПКО', @pNum, @refId, @date, @amt, @base, @pName)";
                            using (var cmdPko = new SQLiteCommand(sqlPKO, conn, trans))
                            {
                                cmdPko.Parameters.AddWithValue("@pNum", pkoNumber);
                                cmdPko.Parameters.AddWithValue("@refId", docId);
                                cmdPko.Parameters.AddWithValue("@date", dpDocDate.SelectedDate.Value.ToString("yyyy-MM-dd"));
                                cmdPko.Parameters.AddWithValue("@amt", (double)currentTotalSum);
                                cmdPko.Parameters.AddWithValue("@base", finalBasis);
                                cmdPko.Parameters.AddWithValue("@pName", string.IsNullOrWhiteSpace(cmbMainPerson.Text) ? (object)DBNull.Value : cmbMainPerson.Text.Trim());
                                cmdPko.ExecuteNonQuery();
                            }

                            trans.Commit();
                            string successMessage = $"Создан документ дохода № {txtDocNumber.Text}\nСоздан ПКО № {pkoNumber}";
                            MessageBox.Show(successMessage, "Успешное сохранение", MessageBoxButton.OK, MessageBoxImage.Information);

                            ResetFormForNewDocument();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            MessageBox.Show("Ошибка сохранения транзакции: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения к SQLite: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetFormForNewDocument()
        {
            _docItems.Clear();
            UpdateTotalAmount();

            // ИСПРАВЛЕНИЕ ДЛЯ КОМБОБОКСА: Полностью сбрасываем выбор и очищаем текст
            cmbMainPerson.SelectedIndex = -1;
            cmbMainPerson.Text = string.Empty;

            txtMainBasis.Text = string.Empty;

            DateTime selectedDate = dpDocDate.SelectedDate ?? DateTime.Now;
            dpDocDate.SelectedDate = selectedDate;
            try
            {
                txtDocNumber.Text = _listOfDocsService.GetNextDocNumber(selectedDate, "Д");
            }
            catch
            {
                string datePart = selectedDate.ToString("ddMMyyyy");
                txtDocNumber.Text = $"Д-{datePart}-001";
            }
            txtDocNumber.Focus();
            if (!string.IsNullOrEmpty(txtDocNumber.Text)) txtDocNumber.SelectAll();
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
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
        private void LoadCategoryTree()
        {
            // Явно указываем системную коллекцию List
            System.Collections.Generic.List<IncomeCategoryItem> dbCategories = _categoryService.GetAll();
            if (dbCategories == null) return;

            System.Collections.Generic.List<IncomeCategoryUI> uiList = dbCategories.Select(item => new IncomeCategoryUI { DbItem = item }).ToList();

            foreach (var cat in uiList)
            {
                cat.Children = new System.Collections.Generic.List<IncomeCategoryUI>();
            }

            var categoryMap = uiList.ToDictionary(c => c.id);

            foreach (var category in uiList)
            {
                if (category.parent_id.HasValue && category.parent_id.Value != 0)
                {
                    if (categoryMap.TryGetValue(category.parent_id.Value, out var parent))
                    {
                        parent.Children.Add(category);
                    }
                }
            }

            var rootCategories = uiList.Where(c => c.parent_id == null || c.parent_id == 0).ToList();
            tvCategories.ItemsSource = rootCategories;
        }

        private void LoadPersonalCombo()
        {
            // Прописываем полный путь к списку сотрудников
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
