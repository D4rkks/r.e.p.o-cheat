using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat;

public class MaterialPreserver : MonoBehaviour
{
	private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

	private float checkInterval = 0.1f;

	private float timeUntilNextCheck;

	private bool initialized;

	private float preservationDuration = 10f;

	private float timeElapsed;

	public void PreserveMaterials()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		Renderer[] componentsInChildren = ((Component)this).GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val in componentsInChildren)
		{
			if (!((Object)(object)val != (Object)null) || val.materials.Length == 0)
			{
				continue;
			}
			Material[] array = (Material[])(object)new Material[val.materials.Length];
			for (int j = 0; j < val.materials.Length; j++)
			{
				if ((Object)(object)val.materials[j] != (Object)null)
				{
					array[j] = new Material(val.materials[j]);
				}
			}
			originalMaterials[val] = array;
		}
		initialized = true;
		((Behaviour)this).enabled = true;
		RestoreMaterials();
	}

	private void RestoreMaterials()
	{
		foreach (KeyValuePair<Renderer, Material[]> originalMaterial in originalMaterials)
		{
			Renderer key = originalMaterial.Key;
			Material[] value = originalMaterial.Value;
			if (!((Object)(object)key != (Object)null))
			{
				continue;
			}
			bool flag = false;
			if (key.sharedMaterials.Length != value.Length)
			{
				flag = true;
			}
			else
			{
				for (int i = 0; i < value.Length; i++)
				{
					if ((Object)(object)key.sharedMaterials[i] == (Object)null || (Object)(object)key.sharedMaterials[i].shader != (Object)(object)value[i].shader || ((Object)(object)key.sharedMaterials[i].mainTexture == (Object)null && (Object)(object)value[i].mainTexture != (Object)null))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				key.materials = value;
			}
		}
	}

	private void Update()
	{
		if (initialized)
		{
			timeUntilNextCheck -= Time.deltaTime;
			if (timeUntilNextCheck <= 0f)
			{
				RestoreMaterials();
				timeUntilNextCheck = checkInterval;
			}
		}
	}

	private void FixedUpdate()
	{
		if (initialized)
		{
			timeElapsed += Time.fixedDeltaTime;
			if (timeElapsed > preservationDuration)
			{
				((Behaviour)this).enabled = false;
			}
		}
	}
}
