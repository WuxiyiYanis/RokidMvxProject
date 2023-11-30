using MVXUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MVXUnity.UI
{
	public class MvxRemoteFileStreamDemo : MonoBehaviour
	{
		public MvxRemoteFileDataStreamDefinition streamDefinition;

		public Text uiBufferingStateText;
		public Text uiBufferingProgressText;
		public Text uiDownloadSpeedText;
		public Text uiCacheFileSuccessfulText;

		void OnEnable()
		{
			streamDefinition.events.onBufferingStateChanged.AddListener(OnBufferingStateChanged);
			streamDefinition.events.onBufferingProgressChanged.AddListener(OnBufferingProgressChanged);
			streamDefinition.events.onDownloadSpeedChanged.AddListener(OnDownloadSpeedChanged);
			streamDefinition.events.onCacheFileSuccessful.AddListener(OnCacheFileSuccessful);

			streamDefinition.onDefinitionChanged.AddListener(OnDefinitionChanged);
		}

		void OnDisable()
		{
			streamDefinition.events.onBufferingStateChanged.RemoveListener(OnBufferingStateChanged);
			streamDefinition.events.onBufferingProgressChanged.RemoveListener(OnBufferingProgressChanged);
			streamDefinition.events.onDownloadSpeedChanged.RemoveListener(OnDownloadSpeedChanged);
			streamDefinition.events.onCacheFileSuccessful.RemoveListener(OnCacheFileSuccessful);

			streamDefinition.onDefinitionChanged.RemoveListener(OnDefinitionChanged);
		}

		private void OnDefinitionChanged()
		{
			uiCacheFileSuccessfulText.text = "";
			uiDownloadSpeedText.text = "";
			uiBufferingProgressText.text = "";
			uiBufferingStateText.text = "";
		}

		private void OnCacheFileSuccessful()
		{
			uiCacheFileSuccessfulText.text = "Successful";
		}

		private void OnDownloadSpeedChanged(float speed)
		{
			uiDownloadSpeedText.text = $"{speed} KB/s";
		}

		private void OnBufferingProgressChanged(int progress)
		{
			uiBufferingProgressText.text = $"{progress}%";
		}

		private void OnBufferingStateChanged(MvxRemoteFileDataStreamDefinition.BufferState state)
		{
			uiBufferingStateText.text = state.ToString();
		}
	}
}