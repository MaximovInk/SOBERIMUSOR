﻿#if UNITY_5_6_OR_NEWER
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Anima2D
{
	public class OnionLayerGameObjectCreationPolicy : PreviewGameObjectCreationPolicy
	{
		public OnionLayerGameObjectCreationPolicy(GameObject go) : base(go) {}

		public override GameObject Create()
		{
			var l_instance = base.Create();

			if(!l_instance.GetComponent<SortingGroup>())
			{
				l_instance.AddComponent<SortingGroup>();
			}

			return l_instance;
		}
	}

	public class OnionLayer
	{
		GameObject m_PreviewInstance;
		SortingGroup m_SourceSortingGroup;
		Renderer[] m_Renderers;
		MaterialCache[] m_MaterialCache;
		
		public Renderer[] renderers { get { return m_Renderers; } private set { m_Renderers = value; } }
		public MaterialCache[] materialCache { get { return m_MaterialCache; } private set { m_MaterialCache = value; } }

		public GameObject previewInstance {
			get { return m_PreviewInstance; }
		}

		public void SetPreviewInstance(GameObject previewInstance, GameObject sourceGameObject)
		{
			m_SourceSortingGroup = sourceGameObject.GetComponent<SortingGroup>();

			if(m_PreviewInstance != previewInstance)
			{
				Destroy();

				m_PreviewInstance = previewInstance;

				if(m_PreviewInstance)
				{
					InitializeRenderers();
				}
			}

		}
		
		public void Destroy()
		{
			if(m_MaterialCache != null)
			{
				foreach(var materialCache in m_MaterialCache)
				{
					if(materialCache != null)
					{
						materialCache.Destroy();
					}
				}
			}

			m_PreviewInstance = null;
		}
		
		void InitializeRenderers()
		{
			renderers = m_PreviewInstance.GetComponentsInChildren<Renderer>(true);

			if(!m_SourceSortingGroup)
			{
				var editorSortingLayers = EditorExtra.GetSortingLayerNames();
				
				//Sort renderers front to back taking sorting layer and sorting order into account
				var l_renderersOrder = new List< KeyValuePair<Renderer, double> >();
				
				for(var i = 0; i < renderers.Length; ++i)
				{
					var l_renderer = renderers[i];
					var l_sortingOrder = l_renderer.sortingOrder;
					var l_layerIndex = editorSortingLayers.IndexOf(l_renderer.sortingLayerName);

					l_renderersOrder.Add(new KeyValuePair<Renderer, double>(l_renderer,(l_layerIndex * 2.0) + (l_sortingOrder / (double)32767)));
				}
				
				l_renderersOrder = l_renderersOrder.OrderByDescending( (s) => s.Value ).ToList();
				
				//Store renderers in order
				renderers = l_renderersOrder.ConvertAll( s => s.Key ).ToArray();
			}
			
			//Create temp materials for non sprite renderers
			var l_materialCacheList = new List<MaterialCache>();
			
			foreach(var renderer in renderers)
			{
				l_materialCacheList.Add(new MaterialCache(renderer));
			}
			
			materialCache = l_materialCacheList.ToArray();
		}
		
		public void SetDepth(int depth)
		{
			var instanceSG = m_PreviewInstance.GetComponent<SortingGroup>();

			if(m_SourceSortingGroup)
			{
				instanceSG.sortingLayerID = m_SourceSortingGroup.sortingLayerID;
				instanceSG.sortingOrder = m_SourceSortingGroup.sortingOrder - depth;
			}
			else if(renderers != null && renderers.Length > 0)
			{
				var lastRenderer = renderers[renderers.Length-1];

				instanceSG.sortingLayerID = lastRenderer.sortingLayerID;
				instanceSG.sortingOrder = lastRenderer.sortingOrder - depth;
			}
		}
		
		public void SetFrame(int frame, AnimationClip clip)
		{
			if(m_PreviewInstance && clip)
			{
				clip.SampleAnimation(m_PreviewInstance, AnimationWindowExtra.FrameToTime(frame));
				
				IkUtils.UpdateIK(m_PreviewInstance,"",false);
			}
		}
		
		public void SetColor(Color color)
		{
			if(renderers != null)
			{
				foreach(var renderer in renderers)
				{
					var spriteRenderer = renderer as SpriteRenderer;
					
					if(spriteRenderer)
					{
						color.a = spriteRenderer.color.a;
						spriteRenderer.color = color;
					}
				}
			}
			
			if(materialCache != null)
			{
				foreach(var l_materialCache in materialCache)
				{
					l_materialCache.SetColor(color);
				}
			}
		}
		
		public void SetAlpha(float alpha)
		{
			if(renderers != null)
			{
				foreach(var renderer in renderers)
				{
					var spriteRenderer = renderer as SpriteRenderer;
					
					if(spriteRenderer)
					{
						var c = spriteRenderer.color;
						c.a = alpha;
						spriteRenderer.color = c;
					}
				}
			}
			
			if(materialCache != null)
			{
				foreach(var l_materialCache in materialCache)
				{
					l_materialCache.SetAlpha(alpha);
				}
			}
		}
	}
}
#endif