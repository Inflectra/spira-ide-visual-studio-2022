















using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms.ToolKit
{
	public class CollectionControl : Control
	{
		#region Properties

		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<object>), typeof(CollectionControl), new UIPropertyMetadata(null));
		public ObservableCollection<object> Items
		{
			get
			{
				return (ObservableCollection<object>)GetValue(ItemsProperty);
			}
			set
			{
				SetValue(ItemsProperty, value);
			}
		}

		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IList), typeof(CollectionControl), new UIPropertyMetadata(null, OnItemsSourceChanged));
		public IList ItemsSource
		{
			get
			{
				return (IList)GetValue(ItemsSourceProperty);
			}
			set
			{
				SetValue(ItemsSourceProperty, value);
			}
		}

		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			CollectionControl CollectionControl = (CollectionControl)d;
			if (CollectionControl != null)
				CollectionControl.OnItemSourceChanged((IList)e.OldValue, (IList)e.NewValue);
		}

		public void OnItemSourceChanged(IList oldValue, IList newValue)
		{
			if (newValue != null)
			{
				foreach (var item in newValue)
					Items.Add(CreateClone(item));
			}
		}

		public static readonly DependencyProperty ItemsSourceTypeProperty = DependencyProperty.Register("ItemsSourceType", typeof(Type), typeof(CollectionControl), new UIPropertyMetadata(null, new PropertyChangedCallback(ItemsSourceTypeChanged)));
		public Type ItemsSourceType
		{
			get
			{
				return (Type)GetValue(ItemsSourceTypeProperty);
			}
			set
			{
				SetValue(ItemsSourceTypeProperty, value);
			}
		}

		private static void ItemsSourceTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			CollectionControl CollectionControl = (CollectionControl)d;
			if (CollectionControl != null)
				CollectionControl.ItemsSourceTypeChanged((Type)e.OldValue, (Type)e.NewValue);
		}

		protected virtual void ItemsSourceTypeChanged(Type oldValue, Type newValue)
		{
			List<Type> types = new List<Type>();
			Type listType = GetListItemType(newValue);

			if (listType != null)
			{
				types.Add(listType);
			}

			NewItemTypes = types;
		}

		public static readonly DependencyProperty NewItemTypesProperty = DependencyProperty.Register("NewItemTypes", typeof(IList), typeof(CollectionControl), new UIPropertyMetadata(null));
		public IList<Type> NewItemTypes
		{
			get
			{
				return (IList<Type>)GetValue(NewItemTypesProperty);
			}
			set
			{
				SetValue(NewItemTypesProperty, value);
			}
		}

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(object), typeof(CollectionControl), new UIPropertyMetadata(null));
		public object SelectedItem
		{
			get
			{
				return (object)GetValue(SelectedItemProperty);
			}
			set
			{
				SetValue(SelectedItemProperty, value);
			}
		}

		#endregion //Properties

		#region Constructors

		static CollectionControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(CollectionControl), new FrameworkPropertyMetadata(typeof(CollectionControl)));
		}

		public CollectionControl()
		{
			Items = new ObservableCollection<object>();
			CommandBindings.Add(new CommandBinding(ApplicationCommands.New, AddNew, CanAddNew));
			CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, Delete, CanDelete));
			CommandBindings.Add(new CommandBinding(ComponentCommands.MoveDown, MoveDown, CanMoveDown));
			CommandBindings.Add(new CommandBinding(ComponentCommands.MoveUp, MoveUp, CanMoveUp));
		}

		#endregion //Constructors

		#region Commands

		private void AddNew(object sender, ExecutedRoutedEventArgs e)
		{
			var newItem = CreateNewItem((Type)e.Parameter);
			Items.Add(newItem);
			SelectedItem = newItem;
		}

		private void CanAddNew(object sender, CanExecuteRoutedEventArgs e)
		{
			Type t = e.Parameter as Type;
			if (t != null && t.GetConstructor(Type.EmptyTypes) != null)
				e.CanExecute = true;
		}

		private void Delete(object sender, ExecutedRoutedEventArgs e)
		{
			Items.Remove(e.Parameter);
		}

		private void CanDelete(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter != null;
		}

		private void MoveDown(object sender, ExecutedRoutedEventArgs e)
		{
			var selectedItem = e.Parameter;
			var index = Items.IndexOf(selectedItem);
			Items.RemoveAt(index);
			Items.Insert(++index, selectedItem);
			SelectedItem = selectedItem;
		}

		private void CanMoveDown(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Parameter != null && Items.IndexOf(e.Parameter) < (Items.Count - 1))
				e.CanExecute = true;
		}

		private void MoveUp(object sender, ExecutedRoutedEventArgs e)
		{
			var selectedItem = e.Parameter;
			var index = Items.IndexOf(selectedItem);
			Items.RemoveAt(index);
			Items.Insert(--index, selectedItem);
			SelectedItem = selectedItem;
		}

		private void CanMoveUp(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Parameter != null && Items.IndexOf(e.Parameter) > 0)
				e.CanExecute = true;
		}

		#endregion //Commands

		#region Methods

		private static void CopyValues(object source, object destination)
		{
			FieldInfo[] myObjectFields = source.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			foreach (FieldInfo fi in myObjectFields)
			{
				fi.SetValue(destination, fi.GetValue(source));
			}
		}

		private object CreateClone(object source)
		{
			object clone = null;

			Type type = source.GetType();
			clone = Activator.CreateInstance(type);
			CopyValues(source, clone);

			return clone;
		}

		private IList CreateItemsSource()
		{
			IList list = null;

			if (ItemsSourceType != null)
			{
				ConstructorInfo constructor = ItemsSourceType.GetConstructor(Type.EmptyTypes);
				list = (IList)constructor.Invoke(null);
			}

			return list;
		}

		private object CreateNewItem(Type type)
		{
			return Activator.CreateInstance(type);
		}

		internal static Type GetListItemType(Type listType)
		{
			Type iListOfT = listType.GetInterfaces().FirstOrDefault(
			  (i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

			return (iListOfT != null)
			  ? iListOfT.GetGenericArguments()[0]
			  : null;
		}

		public void PersistChanges()
		{
			IList list = ComputeItemsSource();
			if (list == null)
				return;

			//the easiest way to persist changes to the source is to just clear the source list and then add all items to it.
			list.Clear();

			foreach (var item in Items)
			{
				list.Add(item);
			}
		}

		private IList ComputeItemsSource()
		{
			if (ItemsSource == null)
				ItemsSource = CreateItemsSource();

			return ItemsSource;
		}

		#endregion //Methods
	}
}
