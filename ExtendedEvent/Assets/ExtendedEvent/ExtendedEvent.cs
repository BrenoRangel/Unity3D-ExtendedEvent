﻿using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ExtendedEvent {

    [Serializable]
    public class Container {
        public UnityEngine.Object Target;
        public int Index;
        public MemberInfo Info = new MemberInfo();
        public List<Value> Values = new List<Value>();
    }

    [Serializable]
    public class Value {
        public AnimationCurve animationCurveValue;
        public bool boolValue;
        public Bounds boundsValue;
        public Color colorValue;
        public double doubleValue;
        public Enum enumValue;
        public float floatValue;
        public int intValue;
        public long longValue;
        public UnityEngine.Object objectReferenceValue;
        public Quaternion quaternionValue;
        public Rect rectValue;
        public string stringValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
    }

    [Serializable]
    public class MemberInfo {
        public int Type;
        public string DeclaringType;
        public string Name;
        public int Parameters;
        public string[] ParameterTypes;
    }

    private List<Action> callbacks = new List<Action>();

    [SerializeField]
    private List<Container> persistentCallbacks;

    public void AddListener( Action callback ) {
        callbacks.Add( callback );
    }

    public void RemoveAllListeners() {
        callbacks.Clear();
    }

    public void RemoveListener( Action callback ) {
        callbacks.Remove( callback );
    }

    public void Invoke() {
        foreach ( var item in persistentCallbacks ) {
            switch ( item.Info.Type ) {
                case 1:
                    InvokeField( item );
                    break;
                case 2:
                    InvokeProperty( item );
                    break;
                case 3:
                    InvokeMethod( item );
                    break;
            }
        }

        foreach ( var item in callbacks ) {
            item();
        }
    }

    private void InvokeField( Container item ) {

    }

    private void InvokeProperty( Container item ) {

    }

    private void InvokeMethod( Container item ) {
        var target = item.Target;
        if ( target is GameObject ) {
            target = ( (GameObject)target ).GetComponent( item.Info.DeclaringType );
        }

        if ( target == null ) {
            Debug.Log( "Component not found" );
            return;
        }

        var type = target.GetType();

        var types = new Type[item.Info.Parameters];
        for ( int i = 0; i < item.Info.Parameters; i++ ) {
            types[i] = Type.GetType( item.Info.ParameterTypes[i] );
        }

        var method = type.GetMethod( item.Info.Name, types );

        if ( method == null ) {
            Debug.Log( "Method not found" );
            return;
        }

        if ( types.Length == 0 ) {
            method.Invoke( target, null );
        } else {
            var values = new object[types.Length];

            for ( int i = 0; i < values.Length; i++ ) {
                values[i] = GetValue( types[i], item.Values[i] );
            }

            method.Invoke( target, values );
        }
    }

    private object GetValue( Type type, Value value ) {
        if ( type == typeof( int ) ) {
            return value.intValue;
        } else if ( type == typeof( bool ) ) {
            return value.boolValue;
        } else if ( type == typeof( float ) ) {
            return value.floatValue;
        } else if ( type == typeof( string ) ) {
            return value.stringValue;
        } else if ( type == typeof( Color ) ) {
            return value.colorValue;
        } else if ( IsUnityObject( type ) ) {
            return value.objectReferenceValue;
        } else if ( type.IsEnum ) {

        } else if ( type == typeof( Vector2 ) ) {
            return value.vector2Value;
        } else if ( type == typeof( Vector3 ) ) {
            return value.vector3Value;
        } else if ( type == typeof( Vector4 ) ) {
            return value.vector4Value;
        } else if ( type == typeof( Rect ) ) {
            return value.rectValue;
        } else if ( type == typeof( AnimationCurve ) ) {
            return value.animationCurveValue;
        } else if ( type == typeof( Bounds ) ) {
            return value.boundsValue;
        } else if ( type == typeof( Quaternion ) ) {
            return value.quaternionValue;
        }

        return null;
    }

    private bool IsUnityObject( Type type ) {
        if ( type == null )
            return false;

        if ( type == typeof( UnityEngine.Object ) )
            return true;

        return IsUnityObject( type.BaseType );
    }
}
