﻿using UnityEngine;
using UE = UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public sealed partial class Unit : UnitBase
{
	#region Types
	#region Serialized Types
#pragma warning disable 0649
#pragma warning restore 0649
	#endregion // Serialized Types
	#endregion // Types

	#region Fields
	#region Serialized Fields
#pragma warning disable 0649
#pragma warning restore 0649
	#endregion // Serialized Fields
	#endregion // Fields

	#region Properties
	#endregion // Properties

	#region Methods
	protected override void AtPreRegister()
	{
		parts.visual.GetComponentsInChildren<SkinnedMeshRenderer>(
			includeInactive: true,
			result: parts.renderers
		);

		var copy = Instantiate(parts.visual, parts.visual.transform.parent);
		Transform copyRoot = copy.transform;
		// Destroy NestedPrefab component
		Destroy(copy);

		for(int i = 0; i < parts.renderers.Count; i++)
		{
			var rend = parts.renderers[i];
			Transform copyBone = copyRoot.FindDeep(rend.name);
			var bodyPart = copyBone.gameObject.AddComponent<BodyPart>();
			parts.bodyParts.Add(bodyPart);
		}
	}
	#endregion // Methods
}
