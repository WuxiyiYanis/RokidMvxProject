namespace MVXUnity
{
    public class MvxDataStreamSourceRuntime
    {
        public virtual bool StreamEnded()
        {
            return false;
        }

        public virtual void Update() { }
    }
}
