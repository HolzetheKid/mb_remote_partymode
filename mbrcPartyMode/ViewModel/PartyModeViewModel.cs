using mbrcPartyMode.Model;
using mbrcPartyMode.ViewModel.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace mbrcPartyMode.ViewModel
{
    public class PartyModeViewModel : ModelBase
    {
        #region vars

        private readonly PartyModeModel model;
        private ClientViewModel clientViewModel;
        private ClientDetailViewModel clientDetailViewModel;
        private bool isActive = false;
        private ObservableCollection<string> logs;

        private static object _syncLock = new object();

        #endregion vars

        #region constructor

        public PartyModeViewModel()
        {
            this.model = PartyModeModel.Instance;
            this.clientViewModel = new ClientViewModel(model);
            this.clientDetailViewModel = new ClientDetailViewModel(clientViewModel.SelectedClient);
            this.clientViewModel.PropertyChanged += OnPropertyChanged;
            this.model.PropertyChanged += OnPropertyChanged;
            this.isActive = model.Settings.IsActive;
            Logs = new ObservableCollectionEx<ServerMessage>();

            this.SaveCommand = new PartyModeSaveCommand();

            ServerCommandExecuted();

            if (AppDomain.CurrentDomain != null)
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            }

            if (Dispatcher.CurrentDispatcher != null)
            {
                Dispatcher.CurrentDispatcher.UnhandledException += CurrentDispatcher_UnhandledException;
            }
        }

        #endregion constructor

        #region ViewModels

        public ClientViewModel ClientViewModel
        {
            get { return clientViewModel; }
        }

        public ClientDetailViewModel ClientDetailViewModel
        {
            get { return clientDetailViewModel; }
        }

        #endregion ViewModels

        #region Commands

        public ICommand SaveCommand
        {
            get;
            private set;
        }

        #endregion Commands

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PartyModeModel.LastServerMessages))
            { ServerCommandExecuted(); }
            else if (e.PropertyName == nameof(ClientViewModel.SelectedClient))
            { SelectedClientChanged(); }
        }

        private void SelectedClientChanged()
        {
            this.clientDetailViewModel = new ClientDetailViewModel(clientViewModel.SelectedClient);
            OnPropertyChanged(nameof(ClientDetailViewModel));
        }

        private void ServerCommandExecuted()
        {
            lock (_syncLock)
            {
                Logs.AddRange(model.LastServerMessages);
                model.LastServerMessages.Clear();
            }
        }

        public bool IsActive
        {
            get
            {
                return model.Settings.IsActive;
            }
            set
            {
                model.Settings.IsActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public ObservableCollectionEx<ServerMessage> Logs
        {
            get; private set;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ToString());
            // Do something with the exception in e.ExceptionObject
        }

        private static void CurrentDispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ToString());
            // Do something with the exception in e.Exception
        }
    }

    /// <summary>
    /// http://geekswithblogs.net/NewThingsILearned/archive/2008/01/16/have-worker-thread-update-observablecollection-that-is-bound-to-a.aspx
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        // Override the event so this class can access it
        public override event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_suppressNotification) return;
            // Be nice - use BlockReentrancy like MSDN said
            using (BlockReentrancy())
            {
                System.Collections.Specialized.NotifyCollectionChangedEventHandler eventHandler = CollectionChanged;
                if (eventHandler == null) return;

                Delegate[] delegates = eventHandler.GetInvocationList();

                foreach (System.Collections.Specialized.NotifyCollectionChangedEventHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcherObject != null && dispatcherObject.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, e);
                    }
                    else // Execute handler as is
                    {
                        handler(this, e);
                    }
                }
            }
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null) throw new ArgumentNullException("list");

            _suppressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// This class contains a few useful extenders for the ListBox
    /// </summary>
    public class ListViewExtenders : DependencyObject
    {
        #region Properties

        public static readonly DependencyProperty AutoScrollToCurrentItemProperty =
            DependencyProperty.RegisterAttached("AutoScrollToCurrentItem", typeof(bool), typeof(ListViewExtenders), new UIPropertyMetadata(default(bool), OnAutoScrollToCurrentItemChanged));

        /// <summary>
        /// Returns the value of the AutoScrollToCurrentItemProperty
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be returned</param>
        /// <returns>The value of the given property</returns>
        public static bool GetAutoScrollToCurrentItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToCurrentItemProperty);
        }

        /// <summary>
        /// Sets the value of the AutoScrollToCurrentItemProperty
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be set</param>
        /// <param name="value">The value which should be assigned to the AutoScrollToCurrentItemProperty</param>
        public static void SetAutoScrollToCurrentItem(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToCurrentItemProperty, value);
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// This method will be called when the AutoScrollToCurrentItem
        /// property was changed
        /// </summary>
        /// <param name="s">The sender (the ListBox)</param>
        /// <param name="e">Some additional information</param>
        public static void OnAutoScrollToCurrentItemChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var listView = s as ListView;
            if (listView != null)
            {
                var listViewItems = listView.Items;
                if (listViewItems != null)
                {
                    var newValue = (bool)e.NewValue;

                    var autoScrollToCurrentItemWorker = new EventHandler((s1, e2) => OnAutoScrollToCurrentItem(listView, listView.Items.CurrentPosition));

                    if (newValue) listViewItems.CurrentChanged += autoScrollToCurrentItemWorker;
                    else listViewItems.CurrentChanged -= autoScrollToCurrentItemWorker;
                }
            }
        }

        /// <summary>
        /// This method will be called when the ListBox should
        /// be scrolled to the given index
        /// </summary>
        /// <param name="listView">The ListBox which should be scrolled</param>
        /// <param name="index">The index of the item to which it should be scrolled</param>
        public static void OnAutoScrollToCurrentItem(ListView listView, int index)
        {
            if (listView != null && listView.Items != null && listView.Items.Count > index && index >= 0)
                listView.ScrollIntoView(listView.Items[listView.Items.Count - 1]);
        }

        #endregion Events
    }

    ///// <summary>
    ///// http://stackoverflow.com/questions/3813087/listview-scroll-to-last-item-wpf-c-sharp
    ///// </summary>
    /////

    //public static class ListViewExtensions
    //{
    //    public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(ListViewExtensions), new UIPropertyMetadata(OnAutoScrollToEndChanged));
    //    private static readonly DependencyProperty AutoScrollToEndHandlerProperty = DependencyProperty.RegisterAttached("AutoScrollToEndHandler", typeof(NotifyCollectionChangedEventHandler), typeof(ListViewExtensions));

    //    public static bool GetAutoScrollToEnd(DependencyObject obj) => (bool)obj.GetValue(AutoScrollToEndProperty);
    //    public static void SetAutoScrollToEnd(DependencyObject obj, bool value) => obj.SetValue(AutoScrollToEndProperty, value);

    //    private static void OnAutoScrollToEndChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    //    {
    //        var listView = s as ListView;

    //        if (listView == null)
    //            return;

    //        var source = (INotifyCollectionChanged)listView.Items.SourceCollection;

    //        if ((bool)e.NewValue)
    //        {
    //            NotifyCollectionChangedEventHandler scrollToEndHandler = delegate
    //            {
    //                if (listView.Items.Count <= 0)
    //                    return;
    //                listView.Items.MoveCurrentToLast();
    //                listView.ScrollIntoView(listView.Items.CurrentItem);
    //            };

    //            source.CollectionChanged += scrollToEndHandler;

    //            listView.SetValue(AutoScrollToEndHandlerProperty, scrollToEndHandler);
    //        }
    //        else
    //        {
    //            var handler = (NotifyCollectionChangedEventHandler)listView.GetValue(AutoScrollToEndHandlerProperty);

    //            source.CollectionChanged -= handler;
    //        }
    //    }
    //}
}