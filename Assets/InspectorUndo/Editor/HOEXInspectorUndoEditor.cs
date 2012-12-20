using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor( typeof( HOEXInspectorUndo ) )]
public class HOEXInspectorUndoEditor : Editor
{

	// VARS ///////////////////////////////////////////////////
	
	private		HOEXInspectorUndo		src;
	
	// ===================================================================================
	// UNITY METHODS ---------------------------------------------------------------------
	
	private void OnEnable()
	{
		src = target as HOEXInspectorUndo;
		
		EditorApplication.modifierKeysChanged += this.Repaint;
	}
	
	private void OnDisable()
	{
		EditorApplication.modifierKeysChanged -= this.Repaint;
	}
	
	override public void OnInspectorGUI()
	{
		Undo.SetSnapshotTarget( this, "HOEXInspectorUndo" );
		
		src.sampleInt = EditorGUILayout.IntField( "Sample Int", src.sampleInt );
		
		if ( GUI.changed ) {
			Undo.CreateSnapshot();
			Undo.RegisterSnapshot();
		}
		Undo.ClearSnapshotTarget();
	}
	
}
