using Autofac;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace BluetoothChat.Helpers
{
    public interface INavigationService
    {
        void Navigate(ViewModelBase viewModel);

        void Register<TViewModel, TView>() where TViewModel : ViewModelBase;
    }

    public class NavigationService : INavigationService
    {
        //Assuming that you only navigate in the root frame
        private readonly Frame m_root;

        private readonly IComponentContext m_componentContext;

        private readonly IDictionary<Type, Type> m_map = new Dictionary<Type, Type>();

        public NavigationService(Frame navigationFrame)
        {
            m_root = navigationFrame;
        }

        public void Navigate(ViewModelBase viewModel)
        {
            var view = m_map[viewModel.GetType()];
            m_root.Navigate(view, viewModel);
        }

        public void Register<TViewModel, TView>()
            where TViewModel : ViewModelBase
        {
            m_map[typeof(TViewModel)] = typeof(TView);
        }
    }
}