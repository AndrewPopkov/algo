// ***********************************************************************
// Assembly         : RsTools
// Author           : Gorbachev
// Created          : 04-03-2015
//
// Last Modified By : Gorbachev
// Last Modified On : 05-29-2014
// ***********************************************************************
// <copyright file="SimpleCommand.cs" company="R-Style Softlab">
//     Copyright (c) R-Style Softlab. Все права защищены.
// </copyright>
// <summary>Реализация интерфейса ICommand</summary>
// ***********************************************************************
using System;
using System.Windows.Input;

/// <summary>
/// Пространство имен Utils.
/// </summary>
namespace RsTools.Utils
{
    /// <summary>
    /// Реализация интерфейса ICommand.
    /// </summary>
    public class SimpleCommand : ICommand
    {
        /// <summary>
        /// Генерируется когда поизошли изменения, которые могли повлиять на возможность выполнения команды.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        private bool canExecute = true;
        private readonly Func<object, bool> canExecuteFunc = null;
        private readonly Action<object> executeAction = null;

        /// <summary>
        /// Обновляет состояние команды.
        /// </summary>
        public void Update()
        {
            NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Генерируе событие <see cref="CanExecuteChanged"/>.
        /// </summary>
        protected void NotifyCanExecuteChanged()
        {
            if (this.CanExecuteChanged != null)
            {
                this.CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Определяет можно ли выполнять команду в текущем состоянии.
        /// </summary>
        /// <param name="parameter">Параметр команды.</param>
        /// <returns>true команду можно выполнять; иначе, false.</returns>
        public bool CanExecute(object parameter)
        {
            if (canExecuteFunc != null)
                this.canExecute = canExecuteFunc(parameter);

            return this.canExecute;
        }

        /// <summary>
        /// Метод выполняемый при активации команды.
        /// </summary>
        /// <param name="parameter">Параметр команды.</param>
        public void Execute(object parameter = null)
        {
            DoExecute(parameter);
        }

        /// <summary>
        /// Выполняет действия команды.
        /// </summary>
        /// <param name="parameter">Параметр команды.</param>
        protected virtual void DoExecute(object parameter)
        {
            if (CanExecute(parameter) && this.executeAction != null)
            {
                this.executeAction(parameter);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SimpleCommand"/>.
        /// </summary>
        /// <param name="executeAction">Действие команды.</param>
        /// <param name="canExecuteFunc">Функция проверки возможности исполнения команды.</param>
        public SimpleCommand(Action<object> executeAction, Func<object, bool> canExecuteFunc = null)
        {
            this.executeAction = executeAction;
            this.canExecuteFunc = canExecuteFunc;
        }
    }
}
