namespace Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Collections;
    using System.Windows.Media.Imaging;
    using System.Drawing;
    using System.IO;
    using System.Windows.Threading;
    using System.Windows.Data;
    using System.Runtime.CompilerServices;
    using ModuleTwo.Views;
    using System.Windows.Controls.Primitives;

        public static class DragAndDropBehavior
        {  
              
        private static Dictionary<String, List<ItemsControl>> dragSources;
        private static Dictionary<String, List<ItemsControl>> dropTargets;

        // Mouse click coordinates
        private static System.Windows.Point startPoint;

        public static readonly DependencyProperty IsDropTargetProperty =
           DependencyProperty.RegisterAttached("IsDropTarget", typeof(bool),
           typeof(DragAndDropBehavior),
           new UIPropertyMetadata(false, IsDropTargetUpdated));

        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.RegisterAttached("IsDragSource", typeof(bool),
            typeof(DragAndDropBehavior),
            new UIPropertyMetadata(false, IsDragSourceUpdated));

        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.RegisterAttached("GroupName", typeof(String),
            typeof(DragAndDropBehavior),
            new UIPropertyMetadata("", GroupNameUpdated));
        
        public static String GetGroupName(DependencyObject obj)
        {
            return (String)obj.GetValue(GroupNameProperty);
        }

        public static void SetGroupName(DependencyObject obj, String value)
        {
            obj.SetValue(GroupNameProperty, value);
        }

        public static bool GetIsDropTarget(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDropTargetProperty);
        }

        public static void SetIsDropTarget(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDropTargetProperty, value);
        }
        
        public static bool GetIsDragSource(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragSourceProperty);
        }

        public static void SetIsDragSource(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragSourceProperty, value);
        }

        private static void InitializeDragDropCollections()
        {
            if (dragSources == null)
                dragSources = new Dictionary<string, List<ItemsControl>>();
            if (dropTargets == null)
                dropTargets = new Dictionary<string, List<ItemsControl>>();
        
            if(!dragSources.Any(p => p.Key == ""))
                dragSources.Add("", new List<ItemsControl>());
            if (!dropTargets.Any(p => p.Key == ""))
            dropTargets.Add("", new List<ItemsControl>());
        }

        // Adds passed ItemsControl to the drag sources collection
        private static void IsDragSourceUpdated(DependencyObject dp,
            DependencyPropertyChangedEventArgs args)
        {
            var isDragSourceEnabled = (bool)args.NewValue;
            var dragSource = dp as ItemsControl;
                       
            if (isDragSourceEnabled)
            {
                Add(dragSources, dragSource);
                dragSource.PreviewMouseMove += OnMouseMove;
                dragSource.PreviewMouseLeftButtonDown += OnLeftButtonDown;
            }
            else
            {
                Remove(dragSources, dragSource);
                dragSource.PreviewMouseMove -= OnMouseMove;
                dragSource.PreviewMouseLeftButtonDown -= OnLeftButtonDown;
            }
        }
        
        // Adds passed ItemsControl to the drop targets collection
        private static void IsDropTargetUpdated(DependencyObject dp,
            DependencyPropertyChangedEventArgs args)
        {
            var isDropTargetEnabled = (bool)args.NewValue;
            var dropTarget = dp as ItemsControl;
        
            dropTarget.AllowDrop = isDropTargetEnabled;            
        
            if (isDropTargetEnabled)
            {
                Add(dropTargets, dropTarget);
                dropTarget.Drop += Drop;
            }
            else
            {
                Remove(dropTargets, dropTarget);
                dropTarget.Drop -= Drop;
            }         
        }
        
        // Adds item control to a group
        // (this item control will only be allowed to participate in d&d in this particular gorup)
        private static void Add(Dictionary<String, List<ItemsControl>> dictionary, object sender)
        {
            InitializeDragDropCollections();
        
            var dp = sender as DependencyObject;
            var itemsControl = sender as ItemsControl;
            var groupName = GetGroupName(dp);
        
            var foundGroup = dictionary.FirstOrDefault(p => p.Key == groupName);
            if (!foundGroup.Value.Contains(itemsControl))
                dictionary[groupName].Add(itemsControl);
        
            itemsControl.Unloaded += DropTarget_Unloaded;
            itemsControl.Loaded += DropTarget_Loaded;
        }
        
        // Removes item control from group
        private static void Remove(Dictionary<String, List<ItemsControl>> dictionary, object sender)
        {
            var dp = sender as DependencyObject;
            var itemsControl = sender as ItemsControl;
            var groupName = GetGroupName(dp);
        
            var foundGroup = dictionary.FirstOrDefault(p => p.Key == groupName);
            if (foundGroup.Value.Contains(itemsControl))
                dictionary[groupName].Remove(itemsControl);
        
            itemsControl.Unloaded -= DropTarget_Unloaded;
            itemsControl.Loaded -= DropTarget_Loaded;
        }

        private static void GroupNameUpdated(DependencyObject dp, DependencyPropertyChangedEventArgs args)
        {
            var itemsControl = dp as ItemsControl;
            string newGroupName = (string)args.NewValue;

            InitializeDragDropCollections();

            if (!dragSources.Any(p => p.Key == newGroupName))
                dragSources.Add((String)args.NewValue, new List<ItemsControl>());
            if (!dropTargets.Any(p => p.Key == newGroupName))
                dropTargets.Add((String)args.NewValue, new List<ItemsControl>());

            var foundCollection = dragSources.Where(p => p.Value.Any(k => k == itemsControl) == true);
            if (foundCollection != null && foundCollection.Count() > 0)
            {
                foundCollection.First().Value.Remove(itemsControl);
                if (!dragSources[((String)args.NewValue)].Any(p => p == itemsControl))
                    dragSources[((String)args.NewValue)].Add(itemsControl);
            }            
        }

        private static void Drop(object sender, DragEventArgs e)
        {
            ItemsControl parent = (ItemsControl)sender;

            // Checking if there's a group that has control assigned to it
            if (!dragSources[GetGroupName(parent as DependencyObject)].Any(p => p == parent))
                return;
            
            // Get the type of data that's used for the object transfered between containers
            var dataType = parent.ItemsSource.GetType().GetGenericArguments().Single(); 
            // Aquiring data of the particular type
            var data = e.Data.GetData(dataType);                                        

            // This will hit when we'll try to drop garbage into the container
            if (data == null)   
                return;

            // We don't wanna drop the same data to the same control again
            if (((IList)parent.ItemsSource).Contains(data)) 
                return;

            var foundControl = dragSources[GetGroupName(parent as DependencyObject)]
                .Find(p => ((IList)p.ItemsSource).Contains(data));

            if (foundControl == null)
                return;

            ((IList)foundControl.ItemsSource).Remove(data);
            ((IList)parent.ItemsSource).Add(data);
            BindingOperations.GetBindingExpressionBase(parent as DependencyObject, 
                ItemsControl.ItemsSourceProperty).UpdateTarget();
            BindingOperations.GetBindingExpressionBase(foundControl as DependencyObject,
                ItemsControl.ItemsSourceProperty).UpdateTarget();
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            ItemsControl source = (ItemsControl)sender;

            System.Windows.Point currentPos = e.GetPosition(null);
            Vector diff = startPoint - currentPos;

            if (e.LeftButton == MouseButtonState.Pressed &&
               ( Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {                
                object data = GetDataFromSource(source, e.GetPosition(source));

                if (data != null)
                {
                    DragDrop.DoDragDrop(source as DependencyObject, data, DragDropEffects.Move);
                }                
            }
        }

        private static object GetDataFromSource(ItemsControl source, System.Windows.Point point)
        {
            UIElement element = source.InputHitTest(point) as UIElement;

            if (element != null)
            {   
                object data = DependencyProperty.UnsetValue;
                while (data == DependencyProperty.UnsetValue)
                {
                    element = VisualTreeHelper.GetParent(element) as UIElement;
                    if (element == source)                    
                        return null;
                    
                    data = source.ItemContainerGenerator.ItemFromContainer(element);                    
                }

                if (data != DependencyProperty.UnsetValue)                
                    return data;                
            }
            return null;
        }

        private static void OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private static void DragSource_Loaded(object sender, RoutedEventArgs e)
        {
            Add(dragSources, sender);
        }
        
        private static void DragSource_Unloaded(object sender, RoutedEventArgs e)
        {
            Remove(dragSources, sender);
        }
        
        private static void DropTarget_Loaded(object sender, RoutedEventArgs e)
        {
            Add(dropTargets, sender);
        }
        
        private static void DropTarget_Unloaded(object sender, RoutedEventArgs e)
        {
            Remove(dropTargets, sender);
        }
    }
}
