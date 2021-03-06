﻿using UnityEngine;
using UE = UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "SA/Sound/Sound")]
public sealed class Sound : ScriptableObject
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
	[SerializeField]
	public AudioClip[] clips;
	[SerializeField]
	public SoundChannelField channel;
	[SerializeField]
	public FloatRange volume = new FloatRange(1.0f, 1.0f);
	[SerializeField]
	public FloatRange pitch = new FloatRange(1.0f, 1.0f);
	[SerializeField]
	public bool loop;
	[SerializeField]
	public float fadeInTime;
	[SerializeField]
	public float fadeOutTime;
	[SerializeField]
	public bool force2D;
	[SerializeField]
	public bool bypassTimeScale;
	#pragma warning restore 0649
	#endregion // Serialized Fields
	#endregion // Fields

	#region Properties
	#endregion // Properties

	#region Mono
	#endregion // Mono

	#region Methods
	#endregion // Methods
}
