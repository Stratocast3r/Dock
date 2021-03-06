﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model;

namespace Dock.Avalonia
{
    internal class AdornerHelper
    {
        public Control Adorner;

        public void AddAdorner(IVisual visual)
        {
            var layer = AdornerLayer.GetAdornerLayer(visual);
            if (layer != null)
            {
                if (Adorner?.Parent is Panel panel)
                {
                    layer.Children.Remove(Adorner);
                    Adorner = null;
                }

                Adorner = new DockTarget
                {
                    [AdornerLayer.AdornedElementProperty] = visual,
                };

                ((ISetLogicalParent)Adorner).SetParent(visual as ILogical);

                layer.Children.Add(Adorner);
            }
        }

        public void RemoveAdorner(IVisual visual)
        {
            var layer = AdornerLayer.GetAdornerLayer(visual);
            if (layer != null)
            {
                if (Adorner?.Parent is Panel panel)
                {
                    layer.Children.Remove(Adorner);
                    ((ISetLogicalParent)Adorner).SetParent(null);
                    Adorner = null;
                }
            }
        }
    }

    /// <summary>
    /// Dock drop handler.
    /// </summary>
    public class DockDropHandler : IDropHandler
    {
        private IDockManager _manager = new DockManager();
        private AdornerHelper _adornerHelper = new AdornerHelper();
        private bool _executed = false;

        /// <summary>
        /// Gets or sets handler id.
        /// </summary>
        public int Id { get; set; }

        private DragAction ToDragAction(DragEventArgs e)
        {
            if (e.DragEffects == DragDropEffects.Copy)
            {
                return DragAction.Copy;
            }
            else if (e.DragEffects == DragDropEffects.Move)
            {
                return DragAction.Move;
            }
            else if (e.DragEffects == DragDropEffects.Link)
            {
                return DragAction.Link;
            }
            return DragAction.None;
        }

        private DockPoint ToDockPoint(Point point)
        {
            return new DockPoint(point.X, point.Y);
        }

        /// <inheritdoc/>
        public void Enter(object sender, DragEventArgs e, object sourceContext, object targetContext)
        {
            var operation = DockOperation.Fill;
            bool isView = sourceContext is IView view;

            if (Validate(sender, e, sourceContext, targetContext, operation) == false)
            {
                if (!isView)
                {
                    e.DragEffects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
            else
            {
                if (isView && sender is DockPanel panel)
                {
                    if (sender is IVisual visual)
                    {
                        _adornerHelper.AddAdorner(visual);
                    }
                }

                e.DragEffects |= DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        public void Over(object sender, DragEventArgs e, object sourceContext, object targetContext)
        {
            bool isView = sourceContext is IView view;
            var operation = DockOperation.Fill;

            if (_adornerHelper.Adorner is DockTarget target)
            {
                operation = target.GetDockOperation(e);
            }

            if (Validate(sender, e, sourceContext, targetContext, operation) == false)
            {
                if (!isView)
                {
                    e.DragEffects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
            else
            {
                e.DragEffects |= DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        public void Drop(object sender, DragEventArgs e, object sourceContext, object targetContext)
        {
            var operation = DockOperation.Fill;
            bool isView = sourceContext is IView view;

            if (_adornerHelper.Adorner is DockTarget target)
            {
                operation = target.GetDockOperation(e);
            }

            if (isView && sender is DockPanel panel)
            {
                _adornerHelper.RemoveAdorner(sender as IVisual);
            }

            if (Execute(sender, e, targetContext, sourceContext, operation) == false)
            {
                if (!isView)
                {
                    e.DragEffects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
            else
            {
                e.DragEffects |= DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        public void Leave(object sender, RoutedEventArgs e)
        {
            _adornerHelper.RemoveAdorner(sender as IVisual);
            Cancel(sender, e);
        }

        /// <inheritdoc/>
        public bool Validate(object sender, DragEventArgs e, object sourceContext, object targetContext, object state)
        {
            if (state is DockOperation operation && sourceContext is IView sourceView && targetContext is IView targetView)
            {
                _manager.Position = ToDockPoint(DropHelper.GetPosition(sender, e));
                _manager.ScreenPosition = ToDockPoint(DropHelper.GetPositionScreen(sender, e));
#if DEBUG
                Console.WriteLine($"Validate [{Id}]: {sourceView.Title} -> {targetView.Title} [{operation}] [{_manager.Position}] [{_manager.ScreenPosition}]");
#endif
                return _manager.Validate(sourceView, targetView, ToDragAction(e), operation, false);
            }
            return false;
        }

        /// <inheritdoc/>
        public bool Execute(object sender, DragEventArgs e, object targetContext, object sourceContext, object state)
        {
            if (_executed == false && state is DockOperation operation && sourceContext is IView sourceView && targetContext is IView targetView)
            {
                _manager.Position = ToDockPoint(DropHelper.GetPosition(sender, e));
                _manager.ScreenPosition = ToDockPoint(DropHelper.GetPositionScreen(sender, e));
#if DEBUG
                Console.WriteLine($"Execute [{Id}]: {sourceView.Title} -> {targetView.Title} [{operation}] [{_manager.Position}] [{_manager.ScreenPosition}]");
#endif
                bool bResult = _manager.Validate(sourceView, targetView, ToDragAction(e), operation, true);
                if (bResult == true)
                {
#if DEBUG
                    Console.WriteLine($"Executed [{Id}]: {sourceView.Title} -> {targetView.Title} [{operation}] [{_manager.Position}] [{_manager.ScreenPosition}]");
#endif
                    _executed = true;
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <inheritdoc/>
        public void Cancel(object sender, RoutedEventArgs e)
        {
            _executed = false;
        }
    }
}
