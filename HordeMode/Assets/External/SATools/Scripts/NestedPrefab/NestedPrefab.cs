﻿using UnityEngine;
using UE = UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

public partial class NestedPrefab : MonoBehaviour
{
	#region Types
	[Flags]
	enum InheritFlags
	{
		StaticFlags = 1 << 0,
		Tag = 1 << 1,
		Layer = 1 << 2,
	}
	#endregion // Types

	#region Fields
	#region Serialized Fields
#pragma warning disable 0649
	[SerializeField]
	GameObject prefab;

	// What flags should all children inherit?
	[SerializeField, EnumFlags]
	InheritFlags inheritFlags = InheritFlags.StaticFlags;

	[SerializeField]
	protected GameObject instantiated;

	[SerializeField]
	OptionalVector3 positionOffset;
	[SerializeField]
	OptionalVector3 rotationOffset;
	[SerializeField]
	OptionalVector3 localScale = new OptionalVector3(new Vector3(1.0f, 1.0f, 1.0f));
#pragma warning restore 0649
	#endregion // Serialized Fields

	bool isSpawning;
	#endregion // Fields

	#region Properties
	#endregion // Properties

	#region Methods
	#region Mono
	protected void Awake()
	{
		if(instantiated == null)
		{
			Spawn();
		}
		else if(Application.isPlaying)
		{
			Setup();
		}
	}
	#endregion // Mono

	[ContextMenu("Respawn")]
	public void Respawn()
	{
		if(instantiated != null)
		{
#if UNITY_EDITOR
			if(EditorApplication.isPlaying)
			{
				Destroy(instantiated);
			}
			else
			{
				DestroyImmediate(instantiated);
			}
#else
			Destroy(instantiated);
#endif // UNITY_EDITOR
			instantiated = null;
		}

		Spawn();
	}

	// Spawn breakdown:
	// - Move to top of NestedPrefab hierarchy
	// - Enqueue top node
	// - - Instantiate node
	// - - Apply flags on each child
	// - - Enqueue each NestedPrefab child
	// - - Repeat
	[ContextMenu("Spawn")]
	public void Spawn()
	{
		if(instantiated != null || isSpawning) { return; }

		NestedPrefab rootParent = FindRootNested(this);

		if(rootParent != null && rootParent != this)
		{
			rootParent.Spawn();
			return;
		}

		isSpawning = true;

		//Dbg.LogRelease(this, "Spawning from {0}", this);

		using(var previouslyInstantiated = TempList<GameObject>.Get())
		{
			WalkRecursive(transform, previouslyInstantiated.buffer);
		}

		isSpawning = false;
	}

	static NestedPrefab FindRootNested(NestedPrefab prefabInstance)
	{
		NestedPrefab rootParent = prefabInstance;

		// Find a parent, any will do (that is not us,
		// GetComponentsInParent includes the same GameObject)
		using(var parents = TempList<NestedPrefab>.Get())
		{
			while(true)
			{
				var thisParent = rootParent;

				rootParent.GetComponentsInParent<NestedPrefab>(
					includeInactive: true,
					results: parents.buffer
				);

				for(int i = parents.Count - 1; i >= 0; --i)
				{
					var parent = parents[i];
					if(parent != thisParent)
					{
						rootParent = parent;
						break;
					}
				}

				// No new parent found
				if(rootParent == thisParent)
				{
					break;
				}
			}
		}

		return rootParent;
	}

	static void WalkRecursive(
		Transform transform,
		List<GameObject> previouslyInstantiated
	)
	{
		bool spawned = false;
		var prefabInstance = transform.GetComponent<NestedPrefab>();

		if(prefabInstance != null)
		{
			spawned = SpawnInternal(prefabInstance, previouslyInstantiated);
		}

		if(spawned)
		{
			previouslyInstantiated.Add(prefabInstance.prefab);
		}

		int childCount = transform.childCount;
		for(int i = 0; i < childCount; ++i)
		{
			Transform child = transform.GetChild(i);
			WalkRecursive(child, previouslyInstantiated);
		}

		if(spawned)
		{
			previouslyInstantiated.RemoveAt(previouslyInstantiated.Count - 1);
		}
	}

	static bool SpawnInternal(
		NestedPrefab prefabInstance,
		List<GameObject> previouslyInstantiated
	)
	{
		GameObject instantiated = prefabInstance.InstantiateSelf(previouslyInstantiated);

		if(instantiated == null) { return false; }

		prefabInstance.Setup();

		// BUG: If not set dirty here .instantiated is not properly saved
#if UNITY_EDITOR
		if(!Application.isPlaying)
		{
			EditorUtility.SetDirty(prefabInstance);
			EditorUtility.SetDirty(prefabInstance.gameObject);
		}
#endif // UNITY_EDITOR

		return true;
	}

	protected void Setup()
	{
		ApplyFlagsRecursive();

		if(positionOffset.hasValue)
		{
			instantiated.transform.localPosition = positionOffset.value;
		}

		if(rotationOffset.hasValue)
		{
			instantiated.transform.localRotation = Quaternion.Euler(rotationOffset.value);
		}

		if(localScale.hasValue)
		{
			instantiated.transform.localScale = localScale.value;
		}

		AtSetup();
	}

	protected virtual void AtSetup()
	{
	}

	void ApplyFlagsRecursive()
	{
		ApplyFlagsRecursive(
			inheritFlags,
			gameObject.layer,
			tag: (0 != (inheritFlags & InheritFlags.Tag)) ? gameObject.tag : null,
			staticFlags:
#if UNITY_EDITOR
					(0 != (inheritFlags & InheritFlags.StaticFlags)) && !Application.isPlaying ?
					(int)GameObjectUtility.GetStaticEditorFlags(gameObject) :
#endif // UNITY_EDITOR
						0,
			trans: instantiated.transform
		);
	}

	static void ApplyFlagsRecursive(
		InheritFlags inheritFlags,
		int layer,
		string tag,
		int staticFlags,
		Transform trans
	)
	{
		bool dirty = false;

		GameObject to = trans.gameObject;

		if(0 != (inheritFlags & InheritFlags.Layer))
		{
			if(to.layer != layer)
			{
				dirty = true;
				to.layer = layer;
			}
		}

		if(0 != (inheritFlags & InheritFlags.Tag))
		{
			if(!to.CompareTag(tag))
			{
				dirty = true;
				to.tag = tag;
			}
		}

#if UNITY_EDITOR
		if(0 != (inheritFlags & InheritFlags.StaticFlags))
		{
			var flags = (StaticEditorFlags)staticFlags;
			var currFlags = GameObjectUtility.GetStaticEditorFlags(to);

			if(flags != currFlags)
			{
				dirty = true;
				GameObjectUtility.SetStaticEditorFlags(to, flags);
			}
		}

		if(dirty)
		{
			EditorUtility.SetDirty(to);
		}
#endif // UNITY_EDITOR

		for(int i = 0; i < trans.childCount; ++i)
		{
			Transform child = trans.GetChild(i);
			ApplyFlagsRecursive(
				inheritFlags,
				layer,
				tag,
				staticFlags,
				child
			);
		}
	}

	GameObject InstantiateSelf(List<GameObject> previouslyInstantiated)
	{
		if(instantiated != null)
		{
			return instantiated;
		}

		if(prefab == null) { return null; }

		if(previouslyInstantiated.Contains(prefab))
		{
			Dbg.LogError(
				this,
				"{0} was already instantiated in this hierarchy. It will not be instantiated to avoid recursion.",
				this
			);
			return null;
		}

		if(prefab == gameObject)
		{
			Dbg.LogError(
				this,
				"{0} had itself as prefab. It will not be instantiated to avoid recursion.",
				this
			);
			return null;
		}

#if UNITY_EDITOR
		if(EditorApplication.isPlayingOrWillChangePlaymode)
		{
			instantiated = GameObject.Instantiate(prefab);

		}
		else
		{
			instantiated = PrefabUtility.InstantiatePrefab(
				prefab,
				destinationScene: gameObject.scene
			) as GameObject;
		}
#else
		instantiated = GameObject.Instantiate(prefab);
#endif // UNITY_EDITOR

		var instantiatedTrans = instantiated.transform;

		if(instantiated != null)
		{
			Vector3 position = instantiatedTrans.localPosition;
			Quaternion rotation = instantiatedTrans.localRotation;
			Vector3 scale = instantiatedTrans.localScale;

			instantiated.transform.parent = transform;
			instantiated.transform.localPosition = position;
			instantiated.transform.localRotation = rotation;
			instantiated.transform.localScale = scale;
		}

		instantiated.name = prefab.name;

		return instantiated;
	}
	#endregion // Methods
}
