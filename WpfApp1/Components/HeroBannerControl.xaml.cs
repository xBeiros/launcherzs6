using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfApp1.ViewModels;

namespace WpfApp1.Components
{
    /// <summary>
    /// Hero-Banner (rechte Spalte oben) mit synchronisiertem 6-Sekunden-Image-Slider,
    /// vorgeladenen Bitmaps, Crossfade + Zoom-Übergang und VSync-flüssiger GPU-Timer-Leiste.
    /// </summary>
    public partial class HeroBannerControl : UserControl
    {
        private const double SlideDurationSeconds = 6.0;

        private readonly Dictionary<string, BitmapImage> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Stopwatch _slideStopwatch = new();
        private bool _isRenderingHooked;
        private DashboardViewModel? _subscribedVm;

        public HeroBannerControl()
        {
            InitializeComponent();
            DataContextChanged += HeroBannerControl_DataContextChanged;
        }

        private void HeroBannerControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as DashboardViewModel);

            if (_subscribedVm is not null)
            {
                PreloadSlideImages(_subscribedVm);
                SetInitialSlideImage(_subscribedVm.ActiveHeroImageUri);
                StartSlideCycle();
            }
        }

        private void HeroBannerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopSlideCycle();
            DetachViewModel();
        }

        private void HeroBannerControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(e.NewValue as DashboardViewModel);

            if (IsLoaded && _subscribedVm is not null)
            {
                PreloadSlideImages(_subscribedVm);
                SetInitialSlideImage(_subscribedVm.ActiveHeroImageUri);
                StartSlideCycle();
            }
        }

        private void AttachViewModel(DashboardViewModel? vm)
        {
            if (ReferenceEquals(_subscribedVm, vm))
            {
                return;
            }

            DetachViewModel();
            _subscribedVm = vm;

            if (_subscribedVm is not null)
            {
                _subscribedVm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void DetachViewModel()
        {
            if (_subscribedVm is not null)
            {
                _subscribedVm.PropertyChanged -= ViewModel_PropertyChanged;
                _subscribedVm = null;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DashboardViewModel.ActiveHeroImageUri) && _subscribedVm is not null)
            {
                TransitionToSlideImage(_subscribedVm.ActiveHeroImageUri);
            }
        }

        private void PreloadSlideImages(DashboardViewModel vm)
        {
            foreach (var slide in vm.HeroSlides)
            {
                GetOrCreateBitmap(slide.ImageUri);
            }
        }

        private void StartSlideCycle()
        {
            _slideStopwatch.Restart();
            if (_subscribedVm is not null)
            {
                _subscribedVm.ActiveSlide.ProgressRatio = 0.0;
            }

            if (!_isRenderingHooked)
            {
                CompositionTarget.Rendering += OnCompositionTargetRendering;
                _isRenderingHooked = true;
            }
        }

        private void StopSlideCycle()
        {
            _slideStopwatch.Stop();
            if (_isRenderingHooked)
            {
                CompositionTarget.Rendering -= OnCompositionTargetRendering;
                _isRenderingHooked = false;
            }
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            if (_subscribedVm is null || !_slideStopwatch.IsRunning)
            {
                return;
            }

            var elapsed = _slideStopwatch.Elapsed.TotalSeconds;
            if (elapsed >= SlideDurationSeconds)
            {
                _slideStopwatch.Restart();
                _subscribedVm.AdvanceHeroSlide();
                _subscribedVm.ActiveSlide.ProgressRatio = 0.0;
                return;
            }

            _subscribedVm.ActiveSlide.ProgressRatio = Math.Clamp(elapsed / SlideDurationSeconds, 0.0, 1.0);
        }

        private void SlideDot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: int slideIndex } && _subscribedVm is not null)
            {
                _subscribedVm.SelectHeroSlide(slideIndex);
                StartSlideCycle();
            }
        }

        private void SetInitialSlideImage(string uriString)
        {
            var bmp = GetOrCreateBitmap(uriString);
            BackSlideImage.Source = bmp;
            FrontSlideImage.Source = bmp;
            FrontSlideImage.Opacity = 1.0;
        }

        private void TransitionToSlideImage(string uriString)
        {
            BackSlideImage.Source = FrontSlideImage.Source;

            var nextBitmap = GetOrCreateBitmap(uriString);
            FrontSlideImage.Source = nextBitmap;
            FrontSlideImage.Opacity = 0.0;

            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(550))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            FrontSlideImage.BeginAnimation(OpacityProperty, fadeIn);

            var zoomX = new DoubleAnimation(1.045, 1.0, TimeSpan.FromMilliseconds(900))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var zoomY = new DoubleAnimation(1.045, 1.0, TimeSpan.FromMilliseconds(900))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            FrontSlideScale.BeginAnimation(ScaleTransform.ScaleXProperty, zoomX);
            FrontSlideScale.BeginAnimation(ScaleTransform.ScaleYProperty, zoomY);
        }

        private BitmapImage GetOrCreateBitmap(string uriString)
        {
            if (_bitmapCache.TryGetValue(uriString, out var cached))
            {
                return cached;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(uriString, UriKind.Absolute);
            bitmap.DecodePixelWidth = 1100;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            _bitmapCache[uriString] = bitmap;
            return bitmap;
        }
    }
}
