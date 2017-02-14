// ***********************************************************************
// Assembly         : DataProvider
// Author           : Chesnokov
// Created          : 01-12-2015
//
// Last Modified By : Chesnokov
// Last Modified On : 03-02-2015
// ***********************************************************************
// <copyright file="RsValidationBase.cs" company="R-Style Softlab">
//     Copyright (c) R-Style Softlab. Все права защищены.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataProvider.DataModel.Validation;
using RsTools.Utils;
using DataProvider.ViewModel.Reflection;
using RsTools.AsyncOperations;
using System.Runtime.Serialization;

namespace DataProvider.ViewModel
{
    /// <summary>
    /// Перечисление типов сообщений модели
    /// </summary>
    public enum MessageSeverity
    {
        /// <summary>
        /// Ошибка валидации
        /// </summary>
        ValidationError,

        /// <summary>
        /// Ошибка времени выполнения
        /// </summary>
        RuntimeError,

        /// <summary>
        /// Предупреждение
        /// </summary>
        Warning,

        /// <summary>
        /// Информационное сообщение
        /// </summary>
        Info
    }

    /// <summary>
    /// Класс MessageInfo. 
    /// </summary>
    public class MessageInfo
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса сообщения <see cref="MessageInfo"/> class.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="severity">Тип сообщения.</param>
        /// <param name="code">Код сообщения.</param>
        /// <param name="help">URL страницы помощи.</param>
        public MessageInfo(string message, MessageSeverity severity = MessageSeverity.ValidationError, int code = 0, string help = "")
        {
            Message = RichTextHelper.MakeXamlFromString(message);
            Severity = severity;
            Code = code;
            Help = help;
            ControlPath = string.Empty;
            Handled = false;
        }

        /// <summary>
        /// Получает или устанавливает тип сообщения.
        /// </summary>
        /// <value>Тип сообщения.</value>
        public MessageSeverity Severity { get; private set; }

        /// <summary>
        /// Получает или устанавливает текст сообщения.
        /// </summary>
        /// <value>Текст сообщения.</value>
        public string Message { get; private set; }

        /// <summary>
        /// Получает или устанавливает код сообщения.
        /// </summary>
        /// <value>Код сообщения.</value>
        public int Code { get; private set; }

        /// <summary>
        /// Получает или устанавливает URL адрес страницы помощи.
        /// </summary>
        /// <value>URL адрес страницы помощи.</value>
        public string Help { get; private set; }

        /// <summary>
        /// Получает или устанавливает путь к компоненту в визуальном дереве.
        /// </summary>
        /// <value>Путь к компоненту в визуальном дереве.</value>
        public string ControlPath { get; set; }

        /// <summary>
        /// Получает или устанавливает признак того, что сообщение обработано.
        /// </summary>
        /// <value><c>true</c>, если сообщение обработано; в противном случае, <c>false</c>.</value>
        public bool Handled { get; set; }

        /// <summary>
        /// Реализация стандартного метода Object.ToString(). Возвращает текст сообщения.
        /// </summary>
        /// <returns>Строка, содержащая текст сообщения.</returns>
        public override string ToString() { return Message; }

        /// <summary>
        /// Метод сортировки по типу сообщений.
        /// </summary>
        /// <param name="a">Первое сообщение.</param>
        /// <param name="b">Второе сообщение.</param>
        /// <returns>0 если оба сообщения имеют одинаковый тип. 
        ///          1 если серьезность первого сообщения выше второго.
        ///          -1 если серьезность первого сообщения ниже второго.</returns>
        public static int SortBySeverity(MessageInfo a, MessageInfo b)
        {
            if ((int)(a.Severity) == (int)(b.Severity))
                return 0;
            if ((int)(a.Severity) < (int)(b.Severity))
                return -1;
            return 1;
        }
    }

    /// <summary>
    /// Класс RsValidationBase. Является базовым классом для классов Model и ViewModel.
    /// </summary>
    public class RsValidationBase : INotifyDataErrorInfo, INotifyPropertyChanged, ICustomTypeProvider
    {
        // будем использовать только для RsDynamicModel, чтобы идентифицировать каждый экземпляр отдельно
        List<CustomPropertyInfo> dynamicCustomProperties;
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RsValidationBase"/> class.
        /// </summary>
        public RsValidationBase()
        {
            _targetType = this.GetType();
        }

        private bool IsDynamicModel
        {
            get { return _targetType == typeof(DataModel.RsDynamicModel); }
        }

        private IBusyIndicator busyIndicator;
        /// <summary>
        /// Получает или устанавливает интерфейс визуализации длительных операций.
        /// </summary>
        /// <value>Класс, реализующий интерйес IBusyIndicator.</value>
        [IgnoreDataMember]
        public IBusyIndicator BusyIndicator
        {
            get { return busyIndicator; }
            set
            {
                var oldValue = busyIndicator;
                busyIndicator = value;
                RaisePropertyChanged("BusyIndicator");
                OnBusyIndicatorChanged(oldValue, busyIndicator);
            }
        }

        /// <summary>
        /// Событие, вызывается при изменении свойства BusyIndicator.
        /// </summary>
        protected virtual void OnBusyIndicatorChanged(IBusyIndicator oldValue, IBusyIndicator newValue) { }

        #region INotifyPropertyChanged

        /// <summary>
        /// Событие, вызывается при изменении значений свойств.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                throw new ArgumentNullException(propertyExpression.Name);
            }

            var memberExpression = propertyExpression.Body as MemberExpression;

            if (memberExpression == null)
            {
                throw new ArgumentException("The expression is not a member access expression.", propertyExpression.Name);
            }

            var property = memberExpression.Member as PropertyInfo;

            if (property == null)
            {
                throw new ArgumentException("The member access expression does not access a property.", propertyExpression.Name);
            }

            return memberExpression.Member.Name;
        }

        /// <summary>
        /// Вызывает событие PropertyChanged, уведомляющее всех подписчиков данного события об изменении свойства.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyExpression">В качестве свойства необходимо передавать лямбда выражение, принимающее свойство, которое изменилось.</param>
        /// <example>
        /// RaisePropertyChanged(() => ChangedProperty);
        /// </example>
        public void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (PropertyChanged != null)
            {
                RaisePropertyChanged(ExtractPropertyName(propertyExpression));
            }
        }

        /// <summary>
        /// Вызывает событие PropertyChanged, уведомляющее всех подписчиков данного события об изменении свойства с именем name.
        /// </summary>
        /// <param name="name">Имя именившегося свойства.</param>
        public void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        #region Вспомогательные методы для пользоватльской валидации значений

        /// <summary>
        /// Добавить ошибку.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого будет добавлена ошибка. 
        /// Ошибка будет отображаться в GUI-компоненте, связанном через binding с этим свойством.</param>
        /// <param name="message">Текст сообщения об ошибке.</param>
        /// <param name="severity">Тип ошибки.</param>
        public void AddError(string propertyName, string message, MessageSeverity severity)
        {
            AddError(propertyName, message, 0, MessageSeverity.ValidationError);
        }

        /// <summary>
        /// Добавить ошибку.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого будет добавлена ошибка. 
        /// Ошибка будет отображаться в GUI-компоненте, связанном через binding с этим свойством.</param>
        /// <param name="message">Текст сообщения об ошибке.</param>
        /// <param name="code">Код ошибки.</param>
        /// <param name="severity">Тип ошибки.</param>
        public void AddError(string propertyName, string message, int code, MessageSeverity severity)
        {
            AddMessage(propertyName, new MessageInfo(message, severity, code));
        }

        private bool SplitAndFindChildValidation(string propertyName, out RsValidationBase validation, out string propertyTail)
        {
            // делим на части имя свойства, если оно составное ишем по первой части дочерний объект валидации
            // и отдаем оставшуюся часть пути на обработку ниже
            var nodes = DottedPathSplitter.Split(propertyName);
            propertyTail = nodes.Tail;
            validation = null;

            if (!string.IsNullOrEmpty(nodes.Tail) && !string.IsNullOrEmpty(nodes.Head))
            {
                if (validationTargets.ContainsKey(nodes.Head))
                {
                    validation = validationTargets[nodes.Head];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Добавить сообщение.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого будет добавлена сообщение. 
        /// Сообщение будет отображаться в GUI-компоненте, связанном через binding с этим свойством.</param>
        /// <param name="message">Текст сообщения.</param>
        public void AddMessage(string propertyName, MessageInfo message)
        {
            string tail;
            RsValidationBase validation;
            if (SplitAndFindChildValidation(propertyName, out validation, out tail))
            {
                validation.AddMessage(tail, message);
                return;
            }

            List<MessageInfo> errorList = null;
            if (!_errors.TryGetValue(propertyName, out errorList))
            {
                errorList = new List<MessageInfo>();
                _errors.Add(propertyName, errorList);
            }

            // если код указан и уже есть в списке, то пропускаем
            if (message.Code != 0 && errorList.Any(msg => msg.Code == message.Code))
            {
                return;
            }
            errorList.Add(message);
            OnErrorsChanged(propertyName);
        }

        /// <summary>
        /// Добавить ошибку.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого будет добавлена ошибка. 
        /// Сообщение об ошибке будет отображаться в GUI-компоненте, связанном через binding с этим свойством.</param>
        /// <param name="message">Текст сообщения об ошибке.</param>
        /// <param name="code">Код ошибки.</param>
        public void AddError(string propertyName, string message, int code = 0)
        {
            AddError(propertyName, message, code, MessageSeverity.ValidationError);
        }

        /// <summary>
        /// Добавить ошибку времени выполнения.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого будет добавлена ошибка. 
        /// Сообщение об ошибке будет отображаться в GUI-компоненте, связанном через binding с этим свойством.</param>
        /// <param name="message">Текст сообщения об ошибке.</param>
        /// <param name="code">Код ошибки.</param>
        public void AddRuntimeError(string propertyName, string message, int code = 0)
        {
            AddError(propertyName, message, code, MessageSeverity.RuntimeError);
        }

        /// <summary>
        /// Удалить ошибку.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого добавлялась ошибка.</param>
        /// <param name="message">Текст сообщения об ошибке.</param>
        public void RemoveError(string propertyName, string message)
        {
            message = RichTextHelper.MakeXamlFromString(message);
            string tail;
            RsValidationBase validation;
            if (SplitAndFindChildValidation(propertyName, out validation, out tail))
            {
                validation.RemoveError(tail, message);
                return;
            }

            List<MessageInfo> errorList = null;
            if (!_errors.TryGetValue(propertyName, out errorList))
                return;

            errorList.RemoveAll(info => info.Message.Equals(message));
            OnErrorsChanged(propertyName);
        }

        /// <summary>
        /// Удалить ошибку.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого добавлялась ошибка.</param>
        /// <param name="code">Код ошибки.</param>
        public void Remove(string propertyName, int code)
        {
            string tail;
            RsValidationBase validation;
            if (SplitAndFindChildValidation(propertyName, out validation, out tail))
            {
                validation.Remove(tail, code);
                return;
            }

            List<MessageInfo> errorList = null;
            if (!_errors.TryGetValue(propertyName, out errorList))
                return;

            errorList.RemoveAll(info => info.Code == code);
            OnErrorsChanged(propertyName);
        }

        /// <summary>
        /// Добавить предупреждение.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого будет добавлено предупреждение. 
        /// Предупреждение будет отображаться в GUI-компоненте, связанном через binding с этим свойством.</param>
        /// <param name="message">Текст предупреждения.</param>
        /// <param name="code">Код предупреждения.</param>
        public void AddWarning(string propertyName, string message, int code = 0)
        {
            AddError(propertyName, message, code, MessageSeverity.Warning);
        }

        /// <summary>
        /// Добавить информационное сообщение.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого будет добавлено сообщение. 
        /// Сообщение будет отображаться в GUI-компоненте, связанном через binding с этим свойством.</param>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="code">Код сообщения.</param>
        public void AddInfo(string propertyName, string message, int code = 0)
        {
            AddError(propertyName, message, code, MessageSeverity.Info);
        }

        /// <summary>
        /// Удалить все ошибки для указанного свойства.
        /// </summary>
        /// <param name="propertyName">Свойство, для которого будут удалены все ошибки.</param>
        public void ClearErrors(string propertyName)
        {
            string tail;
            RsValidationBase validation;
            if (SplitAndFindChildValidation(propertyName, out validation, out tail))
            {
                validation.ClearErrors(tail);
                return;
            }

            List<MessageInfo> errorList = null;
            if (!_errors.TryGetValue(propertyName, out errorList))
                return;

            errorList.Clear();
            OnErrorsChanged(propertyName);
        }

        /// <summary>
        /// Удалить все ошибки модели.
        /// </summary>
        /// <param name="withChildren">Если присвоено значение <c>true</c>, то будут удалены все ошибки в дочерних элементах (виджетах).</param>
        public void ClearErrors(bool withChildren = true)
        {
            var names = _errors.Keys.ToList();
            _errors.Clear();
            names.ForEach(name => OnErrorsChanged(name));

            if (withChildren)
            {
                validationTargets.Values.ForEach(validation =>
                {
                    validation.ClearErrors(withChildren);
                });
            }
        }

        #endregion

        #region Цепочка валидации

        Dictionary<string, RsValidationBase> validationTargets = new Dictionary<string, RsValidationBase>();

        /// <summary>
        /// Добавляет автоматическое отслеживание валидности дочернего виджета.
        /// </summary>
        /// <param name="name">Имя свойства ViewModel для которого следует добавлять ошибки валидации.</param>
        /// <param name="target">Cвойство ViewModel для которого следует добавлять ошибки валидации.</param>
        /// <exception cref="System.ArgumentException">
        /// name
        /// or
        /// target
        /// </exception>
        public void AddValidationTarget(string name, RsValidationBase target)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name");
            }
            if (target == null)
            {
                throw new ArgumentException("target");
            }

            if (!validationTargets.ContainsKey(name))
            {
                validationTargets.Add(name, target);
                target.ErrorsChanged += ChildErrorsChanged;
            }
        }

        /// <summary>
        /// Отменяет автоматическое отслеживание валидности дочернего виджета.
        /// </summary>
        /// <param name="name">Имя свойства, для которого отменяется автоматическое отслеживание.</param>
        public void RemoveValidationTarget(string name)
        {
            if (validationTargets.ContainsKey(name))
            {
                validationTargets[name].ErrorsChanged -= ChildErrorsChanged;
                validationTargets.Remove(name);
            }
        }

        #endregion

        #region Члены INotifyDataErrorInfo

        private Dictionary<string, List<MessageInfo>> _errors = new Dictionary<string, List<MessageInfo>>();
        private List<ErrorMessage> _questions = new List<ErrorMessage>();

        /// <summary>
        /// Происходит при изменении ошибок для свойства.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged = delegate { };

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void ChildErrorsChanged(object s, DataErrorsChangedEventArgs args)
        {
            // просто уведомление о том, что количество ошибок изменилось.
            OnErrorsChanged(string.Empty);
        }

        /// <summary>
        /// Получить коллекцию ошибок для указанного свойства.
        /// </summary>
        /// <param name="propertyName">Имя свойства, для которого запрашивается коллекция ошибок.</param>
        /// <returns>Коллекция ошибок для указанного свойства.</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            return GetAllErrors(propertyName);
        }

        /// <summary>
        /// Получить список ошибок заданного типа для заданного свойства.
        /// </summary>
        /// <param name="propertyName">Имя свойства, ошибки для которого необходимо получить.</param>
        /// <param name="severity">Требуемый тип ошибок.</param>
        /// <returns>Список ошибок заданного типа для заданного свойства.</returns>
        public List<MessageInfo> GetErrors(string propertyName, MessageSeverity severity)
        {
            string tail;
            RsValidationBase validation;
            if (SplitAndFindChildValidation(propertyName, out validation, out tail))
            {
                return validation.GetErrors(tail, severity); ;
            }

            if (propertyName == null || !_errors.ContainsKey(propertyName))
                return new List<MessageInfo>();

            return _errors[propertyName].Where(info => info.Severity == severity).ToList();
        }

        /// <summary>
        /// Получить список ошибок для заданного свойства.
        /// </summary>
        /// <param name="propertyName">Имя свойство, для которого необходимо получить список ошибок.</param>
        /// <returns>Список ошибок для заданного свойства.</returns>
        public List<MessageInfo> GetAllErrors(string propertyName)
        {
            string tail;
            RsValidationBase validation;
            if (SplitAndFindChildValidation(propertyName, out validation, out tail))
            {
                return validation.GetAllErrors(tail); ;
            }

            if (propertyName == null || !_errors.ContainsKey(propertyName))
                return new List<MessageInfo>();

            _errors[propertyName].Sort(MessageInfo.SortBySeverity);
            return _errors[propertyName];
        }

        /// <summary>
        /// Получает значение, указывающее, содержит ли модель ошибки.
        /// </summary>
        /// <value><c>true</c>, если данный экземпляр содержит ошибки; в противном случае, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasErrors
        {
            get
            {
                var hasErrors = _errors.Any(pair => pair.Value.Any(info => info.Severity == MessageSeverity.ValidationError));
                return hasErrors || validationTargets.Values.Any(validation => validation.HasErrors);
            }
        }
        #endregion

        #region Ошибки от сервиса

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <param name="withChildren">если присвоено значение <c>true</c>, то [with children].</param>
        /// <returns>List&lt;MessageInfo&gt;.</returns>
        public List<MessageInfo> GetErrors(bool withChildren = true)
        {
            List<MessageInfo> result = new List<MessageInfo>();
            foreach (var pair in _errors)
            {
                result.AddRange(pair.Value);
            }

            if (withChildren)
            {
                validationTargets.Values.ForEach(validation =>
                {
                    result.AddRange(validation.GetErrors());
                });
            }

            return result;
        }

        /// <summary>
        /// Gets the property names.
        /// </summary>
        /// <param name="withChildren">если присвоено значение <c>true</c>, то [with children].</param>
        /// <returns>List&lt;System.String&gt;.</returns>
        public List<string> GetPropertyNames(bool withChildren = true)
        {
            var result = new List<string>(_errors.Keys);

            if (withChildren)
            {
                validationTargets.ForEach(pair =>
                {
                    var fullNames = pair.Value.GetPropertyNames()
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Select(name => DottedPathSplitter.Combine(pair.Key, name));
                    result.AddRange(fullNames);
                });
            }
            return result;
        }

        #endregion

        #region ICustomTypeProvider

        /// <summary>
        /// Возвращает пользовательский тип <see cref="T:System.Type" />, необходимый для механизма динамических свойств.
        /// </summary>
        /// <returns>Пользовательский тип объекта.</returns>
        public Type GetCustomType()
        {
            if (DesignerProperties.IsInDesignTool)
                return null;
            return _customtype ?? (_customtype = new CustomType(_targetType, this));
        }

        #endregion

        #region Поддержка динамических свойств
        /// <summary>
        /// Получает список динамических свойств типа
        /// </summary>
        private List<CustomPropertyInfo> CustomProperties
        {
            get
            {
                List<CustomPropertyInfo> props = null;

                if (IsDynamicModel)
                {
                    // для каждой динамической модели свой набор свойств
                    if (dynamicCustomProperties == null)
                    {
                        dynamicCustomProperties = new List<CustomPropertyInfo>();
                    }
                    props = dynamicCustomProperties;
                }
                else
                {
                    if (!CustomPropertiesByType.TryGetValue(_targetType, out props))
                    {
                        props = new List<CustomPropertyInfo>();
                        CustomPropertiesByType.Add(_targetType, props);
                    }
                }
                return props;
            }
        }

        /// <summary>
        /// Список динамических свойств по типам.
        /// </summary>
        private static readonly Dictionary<Type, List<CustomPropertyInfo>> CustomPropertiesByType = new Dictionary<Type, List<CustomPropertyInfo>>();

        /// <summary>
        /// Словарь значений динамический свойств.
        /// </summary>
        private readonly Dictionary<string, object> _customPropertyValues = new Dictionary<string, object>();

        /// <summary>
        /// Динамический тип.
        /// </summary>
        private CustomType _customtype;

        /// <summary>
        /// Целевой тип
        /// </summary>
        private Type _targetType;

        private static object DefaultTypeValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// Добавить динамическое свойство.
        /// </summary>
        /// <param name="propertyName">Наименование динамического свойства.</param>
        /// <param name="propertyType">Тип динамического свойства.</param>
        /// <param name="attributes">Список атрибутов динамического свойства.</param>
        public void AddProperty(string propertyName, Type propertyType, List<Attribute> attributes = null)
        {
            var property = GetCustomProperty(propertyName);
            if (property == null)
            {
                CustomPropertyInfo propertyInfo = new CustomPropertyInfo(propertyName, propertyType, _targetType, attributes);
                CustomProperties.Add(propertyInfo);
            }
        }

        /// <summary>
        /// Определяет, содержит ли данный экземпляр динамическое свойство с заданным именем.
        /// </summary>
        /// <param name="propertyName">Наименование свойства.</param>
        /// <returns><c>true</c>, если экземпляр содержит динамическое свойство с заданным именем; в противном случае, <c>false</c>.</returns>
        public bool HasCustomProperty(string propertyName)
        {
            return GetCustomProperty(propertyName) != null;
        }

        /// <summary>
        /// Получить описание динамического свойства с заданным именем.
        /// </summary>
        /// <param name="propertyName">Наименование динамического свойства.</param>
        /// <returns>PropertyInfo.</returns>
        public PropertyInfo GetCustomPropertyInfo(string propertyName)
        {
            return GetCustomProperty(propertyName);
        }

        /// <summary>
        /// Получить значение динамического свойства.
        /// </summary>
        /// <param name="propertyName">Наименование динамического свойства.</param>
        /// <returns>Значение динамического свойства.</returns>
        /// <exception cref="System.Exception"></exception>
        public object GetPropertyValue(string propertyName)
        {
            PropertyInfo property = GetPropertyImpl(propertyName);
            if (property == null)
            {
                throw new Exception(String.Format("Не найдено свойство \"{0}\". ", propertyName));
            }
            return property.GetValue(this, null);
        }

        /// <summary>
        /// Задать значение динамического свойства.
        /// </summary>
        /// <param name="propertyName">Наименование динамического свойства.</param>
        /// <param name="value">Новое значение динамического свойства.</param>
        /// <exception cref="System.Exception"></exception>
        public void SetPropertyValue(string propertyName, object value)
        {
            PropertyInfo property = GetPropertyImpl(propertyName);
            if (property == null)
            {
                throw new Exception(String.Format("Не найдено свойство \"{0}\". ", propertyName));
            }
            property.SetValue(this, value, null);
        }

        /// <summary>
        /// Служебный метод для получения значения динамического свойства. Только для внутреннего использования.
        /// </summary>
        /// <param name="propertyName">Наименование динамического свойства.</param>
        /// <returns>Значение динамического свойства.</returns>
        /// <exception cref="System.Exception"></exception>
        internal object GetPropertyValueInternal(string propertyName)
        {
            object customPropertyValue;
            if (!_customPropertyValues.TryGetValue(propertyName, out customPropertyValue))
            {
                //Не удалось найти значение указанного свойства среди значений динамических свойств
                //Проверим, есть ли вообще такое динамическое свойство
                PropertyInfo property = GetCustomProperty(propertyName);
                if (property != null)
                {
                    //динамическое свойство есть, но значение ещё не присваивалось - вернём дефолтное для данного типа свойства
                    customPropertyValue = DefaultTypeValue(property.PropertyType);
                }
                else
                {
                    //динамического свойства нет, попробуем найти и получить значение из реального свойства
                    property = GetRealProperties().FirstOrDefault(prop => prop.Name == propertyName);
                    if (property != null)
                    {
                        customPropertyValue = property.GetValue(this, null);
                    }
                    else
                    {
                        throw new Exception(String.Format("Не найдено свойство \"{0}\". ", propertyName));
                    }
                }
            }
            return customPropertyValue;
        }

        /// <summary>
        /// Служебный метод для задания значения динамического свойства. Только для внутреннего использования.
        /// </summary>
        /// <param name="propertyName">Наименование динамического свойства.</param>
        /// <param name="value">Новое значение динамического свойства.</param>
        /// <exception cref="System.Exception">
        /// Неправильный тип устанавливаемого значения.
        /// or
        /// or
        /// Неправильный тип устанавливаемого значения.
        /// </exception>
        internal void SetPropertyValueInternal(string propertyName, object value)
        {
            object customPropertyValue;
            if (!_customPropertyValues.TryGetValue(propertyName, out customPropertyValue))
            {
                //Не удалось найти значение указанного свойства среди значений динамических свойств
                //Проверим, есть ли вообще такое динамическое свойство
                PropertyInfo property = GetCustomProperty(propertyName);
                if (property != null)
                {
                    //динамическое свойство есть, но значение ещё не присваивалось, установим новое значение
                    _customPropertyValues[propertyName] = value;
                    RaisePropertyChanged(propertyName);
                }
                else
                {
                    //динамического свойства нет, попробуем найти и получить значение из реального свойства
                    property = GetRealProperties().FirstOrDefault(prop => prop.Name == propertyName);
                    if (property != null)
                    {
                        if (ValidateValueType(value, property.PropertyType))
                        {
                            if (property.GetValue(this, null) != value)
                            {
                                property.SetValue(this, value, null);
                                RaisePropertyChanged(propertyName);
                            }
                        }
                        else throw new Exception("Неправильный тип устанавливаемого значения.");
                    }
                    else
                    {
                        throw new Exception(String.Format("Не найдено свойство \"{0}\". ", propertyName));
                    }
                }
            }
            else
            {
                //Удалось найти значение указанного свойства среди значений динамических свойств
                CustomPropertyInfo propertyInfo = GetCustomProperty(propertyName);
                if (ValidateValueType(value, propertyInfo.PropertyType))
                {
                    if (customPropertyValue != value)
                    {
                        _customPropertyValues[propertyName] = value;
                        RaisePropertyChanged(propertyName);
                    }
                }
                else throw new Exception("Неправильный тип устанавливаемого значения.");
            }
        }

        /// <summary>
        /// Получить массив описаний динамических свойств.
        /// </summary>
        /// <returns>Массив описаний динамических свойств.</returns>
        internal PropertyInfo[] GetCustomProperties()
        {
            return CustomProperties.Cast<PropertyInfo>().ToArray();
        }

        /// <summary>
        /// Получить массив описаний реальных (не динамических) свойств.
        /// </summary>
        /// <returns>Массив описаний реальных (не динамических) свойств.</returns>
        internal PropertyInfo[] GetRealProperties()
        {
            return _targetType.GetProperties();
        }

        /// <summary>
        /// Реализация метода получения описания динамического свойства с заданным наименованием.
        /// </summary>
        /// <param name="propertyName">Наименование динамического свойства.</param>
        /// <returns>Описание динамического свойства.</returns>
        internal PropertyInfo GetPropertyImpl(string propertyName)
        {
            var pi = _targetType.GetProperties().FirstOrDefault(prop => prop.Name == propertyName) ??
                        CustomProperties.FirstOrDefault(prop => prop.Name == propertyName);
            return pi;
        }

        private CustomPropertyInfo GetCustomProperty(string propertyName)
        {
            return CustomProperties.FirstOrDefault(cprop => cprop.Name == propertyName);
        }

        private bool ValidateValueType(object value, Type type)
        {
            if (value == null)
            {
                if (!type.IsValueType)
                    return true;
                return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
            }
            return type.IsInstanceOfType(value);
        }

        #endregion
    }

}
