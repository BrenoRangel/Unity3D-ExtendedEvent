﻿using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer( typeof( ExtendedEvent ) )]
public class ExtendedEventPropertyDrawer : PropertyDrawer {

    private ReorderableList rList;
    private ExtendedEvent eEvent;
    private string header = "";
    private ExtendedEvent.GameObjectContainer listener;

    private void RestoreState( SerializedProperty property ) {
        if ( rList == null || eEvent == null ) {
            header = property.name;

            var target = property.serializedObject.targetObject;
            var type = target.GetType();
            var field = type.GetField( property.name );
            eEvent = field.GetValue( target ) as ExtendedEvent;
            foreach ( var item in eEvent.Listeners ) {
                item.Initialize();
            }

            rList = new ReorderableList( eEvent.Listeners, typeof( ExtendedEvent.GameObjectContainer ) );
            rList.draggable = false;
            rList.elementHeight *= 2;
            rList.drawHeaderCallback = DrawHeaderInternal;
            rList.drawElementCallback = DrawElementInternal;
            rList.onAddCallback = AddInternal;
            rList.onRemoveCallback = RemoveInternal;
        }
    }

    public override float GetPropertyHeight( SerializedProperty property, GUIContent label ) {
        if ( rList == null ) return 80f;
        return rList.headerHeight + rList.footerHeight + ( rList.elementHeight * Mathf.Max( rList.list.Count, 1 ) ) + 7f;
    }

    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label ) {
        RestoreState( property );
        rList.DoList( position );
    }

    private void DrawHeaderInternal( Rect rect ) {
        EditorGUI.LabelField( rect, header );
    }

    private void AddInternal( ReorderableList list ) {
        eEvent.Listeners.Add( new ExtendedEvent.GameObjectContainer() );
    }

    private void RemoveInternal( ReorderableList list ) {
        eEvent.Listeners.RemoveAt( list.index );
    }

    private void DrawElementInternal( Rect rect, int index, bool isActive, bool isFocused ) {
        listener = eEvent.Listeners[index];

        rect.yMin += 3f;
        rect.yMax -= 7f;

        var thirdWidth = rect.width / 3;
        var halfHeight = rect.height / 2;

        var gameObjectRect = new Rect( rect.x, rect.y, thirdWidth, halfHeight );
        var dropdownRect = new Rect( rect.x + thirdWidth, rect.y, thirdWidth * 2, halfHeight );
        var bottomRect = new Rect( rect.x, rect.y + halfHeight, rect.width, halfHeight );

        EditorGUI.BeginChangeCheck();
        listener.GameObject = (GameObject)EditorGUI.ObjectField( gameObjectRect, listener.GameObject, typeof( GameObject ), true );
        if ( EditorGUI.EndChangeCheck() ) {
            listener.Reset();
        }

        listener.Index = DropdownList( dropdownRect, listener.Index, listener.List );
        var i = listener.Index;

        if ( i > 1 && listener.GameObject != null ) {
            switch ( listener.Type ) {
                case 0:
                    var field = listener.CurrentField;
                    DrawMember( field, bottomRect );
                    break;
                case 1:
                    var property = listener.CurrentProperty;
                    DrawMember( property, bottomRect );
                    break;
                case 2:
                    var method = listener.CurrentMethod;
                    DrawMethod( method, bottomRect );
                    break;
            }
        }
    }

    private void DrawMember( ExtendedEvent.MemberBase member, Rect rect ) {
        rect.yMax += 3;
        rect.yMin += 3;

        switch ( member.TypeName ) {
            case "String":
                member.StringValue = EditorGUI.TextField( rect, member.StringValue );
                break;
            case "Int32":
                member.IntValue = EditorGUI.IntField( rect, member.IntValue );
                break;
            case "Int64":
                member.LongValue = EditorGUI.LongField( rect, member.LongValue );
                break;
            case "Single":
                member.FloatValue = EditorGUI.FloatField( rect, member.FloatValue );
                break;
            case "Double":
                member.DoubleValue = EditorGUI.DoubleField( rect, member.DoubleValue );
                break;
            case "Boolean":
                member.BoolValue = EditorGUI.Toggle( rect, member.BoolValue );
                break;
            case "Vector2":
                member.Vector2Value = EditorGUI.Vector2Field( rect, "", member.Vector2Value );
                break;
            case "Vector3":
                member.Vector3Value = EditorGUI.Vector3Field( rect, "", member.Vector3Value );
                break;
            case "Vector4":
                rect.y -= 16f;
                member.Vector4Value = EditorGUI.Vector4Field( rect, "", member.Vector4Value );
                break;
            case "Quaternion":
                rect.y -= 16f;
                var v4 = new Vector4( member.QuaternionValue.x, member.QuaternionValue.y, member.QuaternionValue.z, member.QuaternionValue.w );
                v4 = EditorGUI.Vector4Field( rect, "", v4 );
                member.QuaternionValue = new Quaternion( v4.x, v4.y, v4.z, v4.w );
                break;
            case "Bounds":
                ShowWizard<BoundsWizard>( rect, member, "Bounds Editor", 405, 130 );
                break;
            case "Rect":
                ShowWizard<RectWizard>( rect, member, "Rect Editor", 350, 130 );
                break;
            case "Matrix4x4":
                ShowWizard<MatrixWizard>( rect, member, "Matrix Editor", 350, 275 );
                break;
            case "AnimationCurve":
                member.AnimationCurveValue = EditorGUI.CurveField( rect, member.AnimationCurveValue );
                break;
            case "Object":
                member.ObjectValue = EditorGUI.ObjectField( rect, member.ObjectValue, member.Type, true );
                break;
            case "Enum":
                var enumValue = (Enum)Enum.Parse( member.Type, member.EnumNames[member.EnumValue] );
                enumValue = EditorGUI.EnumPopup( rect, enumValue );
                for ( int i = 0; i < member.EnumNames.Length; i++ ) {
                    if ( member.EnumNames[i] == enumValue.ToString() ) {
                        member.EnumValue = i;
                        break;
                    }
                }
                break;
            default:
                EditorGUI.HelpBox( rect, string.Format( "The type {0} is not supported", member.RepresentableType ), MessageType.Warning );
                break;
        }
    }

    private void DrawMethod( ExtendedEvent.Method method, Rect rect ) {
        if ( method.Parameters.Count == 1 ) {
            var parameter = method.Parameters[0];
            DrawMember( parameter, rect );
        } else if ( method.Parameters.Count > 1 ) {
            rect.yMax += 3;
            rect.yMin += 3;

            if ( GUI.Button( rect, "..." ) ) {
                var mwiz = ScriptableWizard.DisplayWizard<MethodWizard>( "Parameter Editor", "Close" );
                mwiz.Method = method;
                mwiz.minSize = new Vector2( 400, 200 );
            }
        }
    }
    
    private void ShowWizard<T>( Rect rect, ExtendedEvent.MemberBase member, string title, float width, float height ) where T : FieldWizard {
        if ( GUI.Button( rect, "..." ) ) {
            var wiz = ScriptableWizard.DisplayWizard<T>( title, "Close" );
            wiz.Member = member;
            wiz.minSize = new Vector2( width, height );
            wiz.maxSize = new Vector2( width, height );
        }
    }

    #region DropDown 
    private static int dropdownHash = "extDropDown".GetHashCode();
    private static GUIStyle dropdownPopupStyle = new GUIStyle( EditorStyles.popup );
    private class DropdownCallbackInfo {
        private const string kMaskMenuChangedMessage = "MaskMenuChangedAyo";
        public static DropdownCallbackInfo instance;
        private readonly int controlID;
        private int selectedIndex;
        private object view;
        private MethodInfo method;

        public DropdownCallbackInfo( int controlID ) {
            this.controlID = controlID;
            var assembly = Assembly.GetAssembly( typeof( EditorGUI ) );
            Type t = assembly.GetType( "UnityEditor.GUIView" );
            var p = t.GetProperty( "current", BindingFlags.Static | BindingFlags.Public );
            view = p.GetValue( null, null );
            method = t.GetMethod( "SendEvent", BindingFlags.NonPublic | BindingFlags.Instance );
        }

        public static int GetSelectedValueForControl( int controlID, int index ) {
            Event current = Event.current;

            if ( current.type == EventType.ExecuteCommand && current.commandName == kMaskMenuChangedMessage ) {
                if ( instance == null ) {
                    Debug.LogError( "Mask menu has no instance" );
                    return index;
                } else if ( instance.controlID == controlID ) {
                    index = instance.selectedIndex;
                    GUI.changed = true;
                    instance = null;
                    GUIUtility.hotControl = GUIUtility.keyboardControl = 0;
                    current.Use();
                }
            }

            return index;
        }

        internal void SetMaskValueDelegate( object userData, string[] options, int selected ) {
            selectedIndex = selected;

            if ( view != null ) {
                method.Invoke( view, new object[] { EditorGUIUtility.CommandEvent( kMaskMenuChangedMessage ) } );
            }
        }
    }
    private static int DropdownList( Rect position, int current, GUIContent[] items ) {
        int controlID = GUIUtility.GetControlID( dropdownHash, FocusType.Native, position );
        var mask = DropdownCallbackInfo.GetSelectedValueForControl( controlID, current );

        var evt = Event.current;
        if ( evt.type == EventType.Repaint ) {
            if ( current >= items.Length || current == -1 ) {
                EditorStyles.popup.Draw( position, new GUIContent( "-" ), controlID, false );
            } else {
                EditorStyles.popup.Draw( position, new GUIContent( items[current] ), controlID, false );
            }
        } else if ( evt.type == EventType.MouseDown && position.Contains( evt.mousePosition ) ) {
            DropdownCallbackInfo.instance = new DropdownCallbackInfo( controlID );
            GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
            evt.Use();
            EditorUtility.DisplayCustomMenu( position, items, current,
                new EditorUtility.SelectMenuItemFunction( DropdownCallbackInfo.instance.SetMaskValueDelegate ), null );
        }

        return mask;
    }
    #endregion
}
