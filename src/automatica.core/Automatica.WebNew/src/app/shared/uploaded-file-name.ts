/**
 * The server stores uploads under a random name and returns it as { fileName }: the import must use that name,
 * the client file name is never a path on the server.
 */
export function getUploadedFileName(uploadEvent: any): string {
  try {
    const response = JSON.parse(uploadEvent?.request?.responseText ?? "null");
    const name = response?.fileName ?? response?.FileName;
    if (name) {
      return name;
    }
  } catch {
    // older server: plain response
  }
  return uploadEvent?.file?.name;
}
