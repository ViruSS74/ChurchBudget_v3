using System;
using System.Windows;
using System.Windows.Input;

namespace ChurchBudget_v3_WPF.Views
{
    // ВАЖНО: Класс должен называться именно IncomeCatEditWindow, а не IncomeCatDirWindow!
    public partial class ExpensesCatEditWindow : Window
    {
        // Свойство для получения введенного имени категории в главном окне
        public string CategoryName => txtCategoryName.Text.Trim();

        private readonly int? _id;

        // Конструктор для создания новой записи
        public ExpensesCatEditWindow(string parentName)
        {
            InitializeComponent();
            _id = null;

            txtParentName.Text = parentName;
            txtCategoryName.Focus(); // Сразу ставим фокус на ввод имени
        }

        // Конструктор для редактирования существующей записи
        public ExpensesCatEditWindow(int id, string currentName, string parentName)
        {
            InitializeComponent();
            _id = id;

            txtParentName.Text = parentName;
            txtCategoryName.Text = currentName;

            txtCategoryName.Focus();
            txtCategoryName.SelectAll(); // Выделяем текст для удобного изменения
        }

        /// <summary>
        /// Обработчик кнопки "Сохранить"
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show("Пожалуйста, введите наименование категории доходов!",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = true; // Сигнализируем главному окну об успехе
            this.Close();
        }

        /// <summary>
        /// Перетаскивание кастомного окна мышкой за TitleBar
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// Кнопка закрытия формы (крестик) или кнопка "Отмена"
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}