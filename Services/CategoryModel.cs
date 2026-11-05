public class CategoryModel
{
    // Эти свойства в точности повторяют колонки вашей таблицы в БД
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int IsActive { get; set; }

    // Поле для иерархии. int? (с вопросом) означает, что может быть NULL
    public int? ParentId { get; set; }

    // Переопределяем метод, чтобы в простых списках (ComboBox) 
    // отображалось название, а не техническое имя класса
    public override string ToString()
    {
        return Name;
    }
}
