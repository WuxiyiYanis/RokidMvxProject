using UnityEngine;
using System.Collections.Generic;
using System.Threading;

namespace MVXUnity
{
    public abstract class MvxAsyncDataProcessor : MonoBehaviour
    {
        #region data 

        [SerializeField, HideInInspector] private MvxDataStream m_mvxStream = null;
        public MvxDataStream mvxStream
        {
            get { return m_mvxStream; }
            set
            {
                if (m_mvxStream == value)
                    return;

                if (isActiveAndEnabled)
                    DetachFromMvxStream();

                m_mvxStream = value;

                if (isActiveAndEnabled)
                    AttachToMvxStream();
            }
        }
        
        private object m_frameLock = new object();
        /// <summary> A temporary storage for asynchronously received frames - they can only be processed in the next Update() execution. </summary>
        private MVCommon.SharedRef<Mvx2API.Frame> m_frameToProcess = null;
        private MVCommon.SharedRef<Mvx2API.Frame> frameToProcess
        {
            get { return m_frameToProcess; }
            set
            {
                if (m_frameToProcess != null)
                    m_frameToProcess.Dispose();

                m_frameToProcess = value;
            }
        }

        [Tooltip("Frame processors, of which if at least one is able to process a frame, this one will skip processing it")]
        public List<MvxAsyncDataProcessor> processorsPreventingThisFromProcessing = new List<MvxAsyncDataProcessor>();

        #endregion

        #region stream

        private void AttachToMvxStream()
        {
            if (m_mvxStream == null)
                return;

            m_mvxStream.onStreamOpen.AddListener(HandleStreamOpen);
            m_mvxStream.onNextFrameReceived.AddListener(HandleNextFrameReceived);

            if (m_mvxStream.isOpen)
            {
                HandleStreamOpen();
                HandleNextFrameReceived(m_mvxStream.lastReceivedFrame);
            }
        }

        private void DetachFromMvxStream()
        {
            if (m_mvxStream == null)
                return;

            m_mvxStream.onStreamOpen.RemoveListener(HandleStreamOpen);
            m_mvxStream.onNextFrameReceived.RemoveListener(HandleNextFrameReceived);
        }

        #endregion

        #region stream handlers

        private void HandleStreamOpen()
        {
            frameToProcess = null;
            ResetProcessedData();

            if (mvxStream.lastReceivedFrame != null)
                HandleNextFrameReceived(mvxStream.lastReceivedFrame);
        }

        private void HandleNextFrameReceived(MVCommon.SharedRef<Mvx2API.Frame> frame)
        {
            if (frame == null)
            {
                lock (m_frameLock)
                    frameToProcess = null;
                return;
            }

            MVCommon.SharedRef<Mvx2API.Frame> newFrame = frame.CloneRef();

            // do not process the frame in this thread, instead store it and wait for the next Update()
            lock (m_frameLock)
                frameToProcess = newFrame;
        }

        private bool AnotherProcessorCanProcessFrame(Mvx2API.Frame frame)
        {
            foreach (MvxAsyncDataProcessor dataProcessor in processorsPreventingThisFromProcessing)
            {
                if (!dataProcessor.isActiveAndEnabled)
                    continue;

                if (dataProcessor.CanProcessFrame(frame))
                    return true;
            }

            return false;
        }

        protected abstract bool CanProcessFrame(Mvx2API.Frame frame);

        /// <summary> Resets data processor's already processed data. </summary>
        protected abstract void ResetProcessedData();

        protected abstract void ProcessNextFrame(MVCommon.SharedRef<Mvx2API.Frame> frame);

        #endregion

        #region MonoBehaviour

        public virtual void Reset()
        {
            mvxStream = GetComponent<MvxDataStream>();
        }

        public virtual void Start()
        {
			DetachFromMvxStream();
            AttachToMvxStream();
        }

        public virtual void OnDestroy()
        {
            DetachFromMvxStream();
        }

        public virtual void Update()
        {
            if (!Monitor.TryEnter(m_frameLock))
                return; // do not process the frame in case another frame is being received - waiting in Update() is unacceptable

            if (frameToProcess != null)
            {
                if (frameToProcess.sharedObj != null)
                {
                    if (CanProcessFrame(frameToProcess.sharedObj))
                    {
                        if (!AnotherProcessorCanProcessFrame(frameToProcess.sharedObj))
                        {
                            try
                            {
                                ProcessNextFrame(frameToProcess);
							}
                            catch(System.Exception ex)
							{
                                Debug.LogException(ex);
							}
                        }
                    }
                }
                frameToProcess = null;
            }

            Monitor.Exit(m_frameLock);
        }

        #endregion
    }
}
