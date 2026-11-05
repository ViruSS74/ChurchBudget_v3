using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input; // Добавлено для корректной работы мыши (TitleBar_MouseDown)
using ChurchBudget_v3_WPF.Models;
using ChurchBudget_v3_WPF.Services;
using static ChurchBudget_v3_WPF.Services.IncomeCategoryService;

namespace ChurchBudget_v3_WPF.Views
{
    public partial class IncomeCatDirWindow : Window
    {
        private readonly IncomeCategoryService _service;
        private List<IncomeCategoryUI> _uiCategories;

        public IncomeCatDirWindow()
        {
            InitializeComponent();
            _service = new IncomeCategoryService();
            this.Loaded += IncomeCatDirWindow_Loaded;
        }

        private void IncomeCatDirWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // 1. Загружаем сырые данные из БД через сервис
                List<IncomeCategoryItem> dbCategories = _service.GetAll();
                if (dbCategories == null || dbCategories.Count == 0)
                {
                    dgIncomeCat.ItemsSource = null;
                    return;
                }

                // 2. Оборачиваем в UI-модели
                List<IncomeCategoryUI> uiList = dbCategories.Select(item => new IncomeCategoryUI
                {
                    DbItem = item
                }).ToList();

                _uiCategories = new List<IncomeCategoryUI>();

                // 3. Находим корни дерева (parent_id равен null или 0)
                var rootCategories = uiList
                    .Where(c => c.parent_id == null || c.parent_id == 0)
                    .ToList();

                // 4. Запускаем рекурсивный обход для формирования правильной структуры
                foreach (var root in rootCategories)
                {
                    BuildFlatTree(root, 0, uiList);
                }

                // 5. Выводим результат в DataGrid
                dgIncomeCat.ItemsSource = _uiCategories;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildFlatTree(IncomeCategoryUI current, int currentLevel, List<IncomeCategoryUI> allCats)
        {
            current.Level = currentLevel;
            _uiCategories.Add(current);

            // Ищем прямых потомков
            var children = allCats.Where(c => c.parent_id == current.id).ToList();
            foreach (var child in children)
            {
                BuildFlatTree(child, currentLevel + 1, allCats);
            }
        }

        // --- ИСПРАВЛЕННЫЕ НАЗВАНИЯ МЕТОДОВ ДЛЯ СООТВЕТСТВИЯ XAML ---

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUi = dgIncomeCat.SelectedItem as IncomeCategoryUI;
            string parentName = (selectedUi != null) ? selectedUi.name : "--- (Корневая)";
            int? parentId = (selectedUi != null) ? (int?)selectedUi.id : null;

            // Вызываем наше новое окно, передавая имя родительской категории
            var f = new IncomeCatEditWindow(parentName);
            if (f.ShowDialog() == true)
            {
                // Вызываем метод сохранения из сервиса
                _service.Save(null, f.CategoryName, parentId);
                LoadData(); // Перезагружаем дерево в DataGrid
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUi = dgIncomeCat.SelectedItem as IncomeCategoryUI;
            if (selectedUi == null)
            {
                MessageBox.Show("Выберите категорию для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string parentName = "--- (Корневая)";
            if (selectedUi.parent_id != null && selectedUi.parent_id != 0)
            {
                var parent = _uiCategories.FirstOrDefault(c => c.id == selectedUi.parent_id);
                if (parent != null) parentName = parent.name;
            }

            // Вызываем окно редактирования, передавая ID, текущее имя и имя родителя
            var f = new IncomeCatEditWindow(selectedUi.id, selectedUi.name, parentName);
            if (f.ShowDialog() == true)
            {
                _service.Save(selectedUi.id, f.CategoryName, selectedUi.parent_id);
                LoadData();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUi = dgIncomeCat.SelectedItem as IncomeCategoryUI;
            if (selectedUi == null) return;

            // Проверка на наличие подкатегорий на стороне UI
            bool hasChildren = _uiCategories.Any(c => c.parent_id == selectedUi.id);
            if (hasChildren)
            {
                MessageBox.Show("Нельзя удалить категорию, у которой есть подкатегории!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (MessageBox.Show($"Удалить категорию '{selectedUi.name}'?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _service.Delete(selectedUi.id);
                LoadData();
            }
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем выделенную строку в таблице
            dgIncomeCat.SelectedItem = null;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}