// Copyright 2014 Christoph Brzozowski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Reflection;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// Registry user setting storage provider.
    /// 
    /// This user setting storage provider uses the system registry to store and restore user settings.
    /// </summary>
    public class RegistryUserSettingsStorageProvider : Interfaces.IUserSettingsStorageProvider
    {
        /// <summary>
        /// Restores the user settings from below "HKCU\Software\{path}".
        /// 
        /// This method reads all compatible public properties of the user settings object from corresponding registry values
        /// beneath the specified key.
        /// </summary>
        /// <param name="path">Path from below which the user settings should be restored.</param>
        /// <param name="settings">User settings object.</param>
        public void Restore( string path, object settings )
        {
            if( settings == null )
                return;
            try
            {
                using( var settingsRootKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey( Path.Combine( "Software", path ) ) )
                {
                    Restore( settings, settingsRootKey );
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Restores settings from a registry key.
        /// </summary>
        /// <param name="settings">Settings object, to which settings should be restored.</param>
        /// <param name="settingsRootKey">Registry key from which the settings should be restored.</param>
        private static void Restore( object settings, Microsoft.Win32.RegistryKey settingsRootKey )
        {
            // Handle read/write properties
            foreach( var property in settings.GetType().GetProperties( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.SetProperty ) )
            {
                try
                {
                    if( property.CanRead && property.CanWrite && property.GetSetMethod() != null )
                    {
                        var propertyType = property.PropertyType;
                        if( propertyType == typeof( string ) )
                        {
                            object value = settingsRootKey.GetValue( property.Name, null );
                            if( value != null )
                                property.SetValue( settings, value.ToString(), null );
                        }
                        else
                        if( propertyType == typeof( bool ) )
                        {
                            object value = settingsRootKey.GetValue( property.Name, null );
                            if( value != null )
                                property.SetValue( settings, ( (int) value ) != 0, null );
                        }
                        else
                        if( propertyType == typeof( int ) )
                        {
                            object value = settingsRootKey.GetValue( property.Name, null );
                            if( value != null )
                                property.SetValue( settings, (int) value, null );
                        }
                        else
                        if( propertyType == typeof( float ) || propertyType == typeof( double ) )
                        {
                            object value = settingsRootKey.GetValue( property.Name, null );
                            if( value != null )
                            {
                                double valueAsDouble;
                                if( double.TryParse( value.ToString(), out valueAsDouble ) )
                                {
                                    if( propertyType == typeof( float ) )
                                        property.SetValue( settings, (float) valueAsDouble, null );
                                    else
                                        property.SetValue( settings, valueAsDouble, null );
                                }
                            }
                        }
                        else
                        {
                            object value = property.GetValue( settings, null );
                            if( value != null && value.GetType() == typeof( byte[] ) )
                            {
                                BinaryFormatter binaryFormatter = new BinaryFormatter();
                                using( MemoryStream binaryStream = new MemoryStream( value as byte[] ) )
                                {
                                    object valueObject = binaryFormatter.Deserialize( binaryStream );
                                    property.SetValue( settings, valueObject, null );
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            // Handle collection properties
            foreach( var property in settings.GetType().GetProperties( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty ) )
            {
                try
                {
                    var propertyType = property.PropertyType;
                    if( propertyType.IsGenericType )
                    {
                        // Handle dictionary with string key
                        var genericDefinition = propertyType.GetGenericTypeDefinition();
                        if( genericDefinition == typeof( IDictionary<,> ) && propertyType.GetGenericArguments()[0] == typeof( string ) )
                        {

                            IEnumerable stringDictionary = property.GetValue( settings, null ) as IEnumerable;
                            if( stringDictionary != null )
                            {
                                foreach( var entry in stringDictionary )
                                {
                                    var key = entry.GetType().InvokeMember( "Key", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public, null, entry, null ) as string;
                                    var value = entry.GetType().InvokeMember( "Value", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public, null, entry, null ) as object;
                                    // Create a sub key from the dictionary key name and store the settings below it
                                    using( var entryKey = settingsRootKey.OpenSubKey( Path.Combine( property.Name, key ) ) )
                                    {
                                        Restore( value, entryKey );
                                    }
                                }

                                using( var entryKey = settingsRootKey.OpenSubKey( property.Name ) )
                                {
                                    if( entryKey != null )
                                    {
                                        foreach( var valueName in entryKey.GetValueNames() )
                                        {
                                            var value = entryKey.GetValue( valueName );
                                            if( value != null && ( value.GetType().IsPrimitive || value.GetType() == typeof( string ) ) )
                                                stringDictionary.GetType().GetMethod( "set_Item" ).Invoke( stringDictionary, new object[] { valueName, value } );
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Stores the user settings below "HKCU\Software\{path}".
        /// 
        /// This method writes all compatible public properties of the user settings object as registry values
        /// beneath the specified key.
        /// </summary>
        /// <param name="path">Path under which the user settings should be stored.</param>
        /// <param name="settings">User settings object.</param>
        public void Store( string path, object settings )
        {
            if( settings != null )
            {
                using( var settingsRootKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( Path.Combine( "Software", path ) ) )
                {
                    Store( settings, settingsRootKey );
                }
            }
        }

        /// <summary>
        /// Stores data of the settings object below the specified registry key.
        /// </summary>
        /// <param name="settings">Settings object, to be stored.</param>
        /// <param name="settingsKey">Registry key below which the settings should be stored.</param>
        private static void Store( object settings, Microsoft.Win32.RegistryKey settingsKey )
        {
            // Handle read/write properties
            foreach( var property in settings.GetType().GetProperties( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.SetProperty ) )
            {
                try
                {
                    if( property.CanRead && property.CanWrite && property.GetSetMethod() != null )
                    {
                        var propertyType = property.PropertyType;
                        var propertyValue = property.GetValue( settings, null );
                        var propertyName = property.Name;
                        StoreValue( settingsKey, propertyName, propertyType, propertyValue );
                    }
                }
                catch
                {
                }
            }

            // Handle collection properties
            foreach( var property in settings.GetType().GetProperties( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty ) )
            {
                try
                {
                    var propertyType = property.PropertyType;
                    if( propertyType.IsGenericType )
                    {
                        // Handle dictionary with string key. 
                        var genericDefinition = propertyType.GetGenericTypeDefinition();
                        if( genericDefinition == typeof( IDictionary<,> ) && propertyType.GetGenericArguments()[0] == typeof( string ) )
                        {
                            IEnumerable stringDictionary = property.GetValue( settings, null ) as IEnumerable;
                            if( stringDictionary != null )
                            {
                                foreach( var entry in stringDictionary )
                                {
                                    var key = entry.GetType().InvokeMember( "Key", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public, null, entry, null ) as string;
                                    var value = entry.GetType().InvokeMember( "Value", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public, null, entry, null ) as object;
                                    
                                    if( value != null && ( value.GetType().IsPrimitive || value.GetType() == typeof(string) ) )
                                    {
                                        using( var entryKey = settingsKey.CreateSubKey( property.Name ) )
                                        {
                                            StoreValue( entryKey, key, value.GetType(), value );
                                        }
                                    }
                                    else
                                    {
                                        // Create a sub key from the dictionary key name and store the settings below it
                                        using( var entryKey = settingsKey.CreateSubKey( Path.Combine( property.Name, key ) ) )
                                        {
                                            Store( value, entryKey );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static void StoreValue( Microsoft.Win32.RegistryKey settingsKey, string propertyName, Type propertyType, object propertyValue )
        {
            if( propertyType == typeof( string ) )
            {
                settingsKey.SetValue( propertyName, propertyValue, Microsoft.Win32.RegistryValueKind.String );
            }
            else
            if( propertyType == typeof( bool ) )
            {
                settingsKey.SetValue( propertyName, (bool) propertyValue ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord );
            }
            else
            if( propertyType == typeof( int ) )
            {
                settingsKey.SetValue( propertyName, (int) propertyValue, Microsoft.Win32.RegistryValueKind.DWord );
            }
            else
            if( propertyType == typeof( float ) || propertyType == typeof( double ) )
            {
                settingsKey.SetValue( propertyName, propertyValue.ToString(), Microsoft.Win32.RegistryValueKind.String );
            }
            else
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                using( MemoryStream binaryStream = new MemoryStream() )
                {
                    binaryFormatter.Serialize( binaryStream, propertyValue );
                    settingsKey.SetValue( propertyName, binaryStream.ToArray(), Microsoft.Win32.RegistryValueKind.Binary );
                }
            }
        }
    }
}
