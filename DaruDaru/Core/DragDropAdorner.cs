using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DaruDaru.Core
{
    internal class DragDropAdorner : Adorner
    {
        /*
        <Canvas Width="76" Height="76" Clip="F1 M 0,0L 76,0L 76,76L 0,76L 0,0">
            <Path Width="36" Height="42" Canvas.Left="20" Canvas.Top="17" Stretch="Fill" Fill="{DynamicResource AccentColorBrush3}" Data="F1 M 20,17L 43.25,17L 56,29.75L 56,59L 20,59L 20,17 Z M 24,21L 24,55L 52,55L 52,34L 39,34L 39,21L 24,21 Z M 43,22.25L 43,30L 50.75,30L 43,22.25 Z M 30,33L 34,33L 34,41.5L 37,38.5L 37,43.25L 32,48.25L 27,42.75L 27,38.5L 30,41.5L 30,33 Z M 38,49L 38,53L 26,53L 26,49L 38,49 Z "/>
        </Canvas>
        */

        private const int DragDropPathWidth  = 36;
        private const int DragDropPathHeight = 42;
        private static readonly Geometry DragDropPath;

        static DragDropAdorner()
        {
            DragDropPath = Geometry.Parse("F1 M 20,17L 43.25,17L 56,29.75L 56,59L 20,59L 20,17 Z M 24,21L 24,55L 52,55L 52,34L 39,34L 39,21L 24,21 Z M 43,22.25L 43,30L 50.75,30L 43,22.25 Z M 30,33L 34,33L 34,41.5L 37,38.5L 37,43.25L 32,48.25L 27,42.75L 27,38.5L 30,41.5L 30,33 Z M 38,49L 38,53L 26,53L 26,49L 38,49 Z ");
            DragDropPath.Freeze();
        }

        private FrameworkElement m_element;
        private Brush m_brush;

        public DragDropAdorner(FrameworkElement element, Brush brush)
            : base(element)
        {
            this.m_element = element;
            this.m_brush = brush;

            this.IsHitTestVisible = false;

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var geometry = DragDropPath.Clone();
            var transform = new TranslateTransform((this.m_element.ActualWidth - DragDropPathWidth) / 2,
                                                   (this.m_element.ActualHeight - DragDropPathHeight) / 2);

            geometry.Transform = transform;
            drawingContext.DrawGeometry(this.m_brush, null, geometry);

        }
    }
}
