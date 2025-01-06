public class CannotOpenUDPPortException : System.Exception {
    public CannotOpenUDPPortException() : base() { }
    public CannotOpenUDPPortException(string message) : base(message) { }
    public CannotOpenUDPPortException(string message,
                                      System.Exception inner) : base(message, inner) { }

    protected CannotOpenUDPPortException(System.Runtime.Serialization.SerializationInfo info,
                                         System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class ListUninitalized : System.Exception {
    public ListUninitalized() : base() { }
    public ListUninitalized(string message) : base(message) { }
}
