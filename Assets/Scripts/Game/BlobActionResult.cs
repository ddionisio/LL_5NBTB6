
public struct BlobActionResult {
	public enum Type {
		None,

		Success,
		Fail,
		Cancel
	}

	public Type type;

	public BlobConnectController.Group group; //group associated with result if available

	public Blob blobDividend;
	public Blob blobDivisor;

	public int newValue;
	public int splitValue;
}
