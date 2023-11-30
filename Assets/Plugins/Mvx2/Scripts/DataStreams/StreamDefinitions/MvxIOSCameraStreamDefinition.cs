using Mvx2API;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MVXUnity
{
    public class MvxIOSCameraStreamDefinition : MvxDataStreamDefinition
    {
        private const string sourceFilterGUID = "7414C3F5-DDCF-4B22-B55C-D371D94C7545";

        public int FPS = 24;
        public int bufferSize = 2;
        public string focusMode = "autoFocus";
        public string focusPointOfInterest = "0.5/0.5";
        public string exposureMode = "autoExpose";
        public string exposurePointOfInterest = "0.5/0.5";
        public string exposureDuration = "0";
        public string ISO = "-1";
        public string whiteBalanceMode = "continuousAutoWhiteBalance";

        public override MvxDataStreamSourceRuntime AppendSource(ManualGraphBuilder graphBuilder)
		{
            Mvx2API.SingleFilterGraphNode singleFilterGraphNode = new Mvx2API.SingleFilterGraphNode(MVCommon.Guid.FromHexString(sourceFilterGUID));
            
            singleFilterGraphNode.SetFilterParameterValue(nameof(FPS), FPS.ToString());
            singleFilterGraphNode.SetFilterParameterValue(nameof(bufferSize), bufferSize.ToString());
            singleFilterGraphNode.SetFilterParameterValue(nameof(focusMode), focusMode);
            singleFilterGraphNode.SetFilterParameterValue(nameof(focusPointOfInterest), focusPointOfInterest);
            singleFilterGraphNode.SetFilterParameterValue(nameof(exposureMode), exposureMode);
            singleFilterGraphNode.SetFilterParameterValue(nameof(exposurePointOfInterest), exposurePointOfInterest);
            singleFilterGraphNode.SetFilterParameterValue(nameof(exposureDuration), exposureDuration);
            singleFilterGraphNode.SetFilterParameterValue(nameof(ISO), ISO);
            singleFilterGraphNode.SetFilterParameterValue(nameof(whiteBalanceMode), whiteBalanceMode);

            graphBuilder.AppendGraphNode(singleFilterGraphNode);
            return new MvxDataStreamSourceRuntime();
        }
    }
}