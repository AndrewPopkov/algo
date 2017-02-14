using DataProvider.ViewModel;
using RsTools.AsyncOperations;
using Categories.DataSources;
using DataProvider.DataModel.Categories;
using System.Collections.Generic;
using RsTools.Utils;

namespace Categories.ViewModel
{
    public class CatHistoryViewModel : RsViewModel
    {
        private CategoriesFacade facade = new CategoriesFacade();
        public CategoriesFacade Facade
        {
            get { return this.facade; }
        }

        /// <summary>
        ///  SimpleCommand.
        /// </summary>
        private SimpleCommand _SaveCategoryTb = null;
        /// <summary>
        ///  Сохранить признаки категории в файл.
        /// </summary>
        public SimpleCommand SaveCategoryTb
        {
            get { return _SaveCategoryTb; }
            set { _SaveCategoryTb = value; RaisePropertyChanged(() => SaveCategoryTb); }
        }

        /// <summary>
        ///  SimpleCommand.
        /// </summary>
        private SimpleCommand _LoadCategoryTb = null;
        /// <summary>
        ///  Загрузить признаки категории из файла.
        /// </summary>
        public SimpleCommand LoadCategoryTb
        {
            get { return _LoadCategoryTb; }
            set { _LoadCategoryTb = value; RaisePropertyChanged(() => LoadCategoryTb); }
        }

        private int _ObjectType;
        /// <summary>
        ///  Вид объекта.
        /// </summary>
        public int ObjectType
        {
            get { return _ObjectType; }
            set
            {
                _ObjectType = value;
                RaisePropertyChanged(() => ObjectType);
            }
        }
        private string _ObjectID;
        /// <summary>
        ///  Ид. объекта.
        /// </summary>
        public string ObjectID
        {
            get { return _ObjectID; }
            set
            {
                _ObjectID = value;
                RaisePropertyChanged(() => ObjectID);
            }
        }

        private List<int> _Groups;
        /// <summary>
        /// Массив групп категорий для объекта, которые необходимо вернуть.
        /// </summary>
        public List<int> Groups
        {
            get { return _Groups; }
            set
            {
                _Groups = value;
                RaisePropertyChanged(() => Groups);
            }
        }

        private List<TWsCategoryValue> _Values;
        /// <summary>
        /// Массив заданных атрибутов категорий для объекта, которые необходимо вернуть. 
        /// </summary>
        public List<TWsCategoryValue> Values
        {
            get { return _Values; }
            set
            {
                _Values = value;
                RaisePropertyChanged(() => Values);
            }
        }

        private bool _IsTransient;
        /// <summary>
        /// Признак работы с временным списком атрибутов Values, не требующим подкачки из БД реальных данных.
        /// </summary>
        public bool IsTransient
        {
            get { return _IsTransient; }
            set
            {
                _IsTransient = value;
                RaisePropertyChanged(() => IsTransient);
            }
        }

        private string _ObjectName;
        /// <summary>
        ///  Наименование объекта.
        /// </summary>
        public string ObjectName
        {
            get { return _ObjectName; }
            set
            {
                _ObjectName = value;
                RaisePropertyChanged(() => ObjectName);
            }
        }

        private string _ObjectTypeName;
        /// <summary>
        ///  Наименование вида объекта.
        /// </summary>
        public string ObjectTypeName
        {
            get { return _ObjectTypeName; }
            set
            {
                _ObjectTypeName = value;
                RaisePropertyChanged(() => ObjectTypeName);
            }
        }

        private TWsCategoryValue _SelectedGridItem;
        /// <summary>
        /// Текущий, выбранный элемент скролинга.
        /// </summary>
        public TWsCategoryValue SelectedGridItem
        {
            get
            {
                return _SelectedGridItem;
            }
            set
            {
                _SelectedGridItem = value;
                RaisePropertyChanged(() => SelectedGridItem);
            }
        }


        private List<TWsCategory> _CategoryCollection;
        /// <summary>
        /// Список категорий объекта.
        /// </summary>
        public List<TWsCategory> CategoryCollection
        {
            get { return _CategoryCollection; }
            set
            {
                _CategoryCollection = value;
                RaisePropertyChanged(() => CategoryCollection);
            }
        }

        private TWsCategory _Category;
        /// <summary>
        /// Информация о категорий объекта.
        /// </summary>
        public TWsCategory Category
        {
            get { return _Category; }
            set
            {
                _Category = value;
                ChangeCategory();
                RaisePropertyChanged(() => Category);
            }
        }


        private CategoryValuesDataSource _DataSource;
        /// <summary>
        /// Получает или устанавливает источник данных для списка записей.
        /// </summary>
        public CategoryValuesDataSource DataSource
        {
            get { return _DataSource; }
            set
            {
                _DataSource = value;

                RaisePropertyChanged(() => DataSource);
            }
        }


        private void ChangeCategory()
        {
            if (ObjectType > 0 && (!string.IsNullOrEmpty(ObjectID)))
            {
                List<int> group = new List<int>();
                group.Add(Category.GroupID);
                DataSource = new CategoryValuesDataSource(null, ObjectType, ObjectID, group, Values, IsTransient, true);
                DataSource.Refresh();
            }
        }

        /// <summary>
        /// Загружает данные в панель.
        /// </summary>
        /// <param name="asyncAdapter">Адаптер асинхронной инициализации.</param>
        /// <param name="_NoteKind">Вид примечания.</param>
        public void LoadData(AsyncInitializationAdapter asyncAdapter, TWsCategory _Category)
        {
            Facade.GetCategoriesList(ObjectType, Groups, true, nkList =>
            {

                CategoryCollection = nkList as List<TWsCategory>;



            });

            Category = _Category;

            if (asyncAdapter != null)
            {
                asyncAdapter.Complete();
            }
        }
    }
}
