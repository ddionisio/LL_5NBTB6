
public struct BlobActionResult {
	public enum Type {
		None,

		Success,
		Fail,
		Cancel
	}

	public Type type;

	public BlobConnectController.Group group; //group associated with result if available

	public Blob blobLeft; //eg. dividend
	public Blob blobRight; //eg. divisor

	public int val;
}
