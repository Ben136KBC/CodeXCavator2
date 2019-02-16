using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// Wrapper fo IOptionsProvider.
    /// 
    /// The wrapper provides option values through the
    /// underlying options provider, but intercepts write
    /// accesses and stores the values locally.
    /// </summary>
    internal class OptionsProviderWrapper : IOptionsProvider
    {
        IOptionsProvider mBaseOptionsProvider;
        Dictionary<string, object> mOptionValues = new Dictionary<string, object>();

        public OptionsProviderWrapper( IOptionsProvider baseOptionsProvider )
        {
            mBaseOptionsProvider = baseOptionsProvider;
        }

        public IEnumerable<IOption> Options
        {
            get { return mBaseOptionsProvider.Options; }
        }

        public void SetOptionValue( IOption option, object value )
        {
            mOptionValues[option.Name] = Convert.ChangeType( value, option.ValueType );
            if( OptionChanged != null )
                OptionChanged( this, option );
        }

        public object GetOptionValue( IOption option )
        {
            object value;
            if( mOptionValues.TryGetValue( option.Name, out value ) )
                return value;
            return mBaseOptionsProvider.GetOptionValue( option );
        }

        public event OptionChangedEvent OptionChanged;
    }
}
