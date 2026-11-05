import { authenticatedFetch } from "../middleware/authMiddleware";

const BASE_URL = (
    import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5138/api"
).replace(/\/+$/, "");

export async function request(path, options = {}) {
    const { skipAuth = false, headers: suppliedHeaders, ...fetchOptions } = options;
    const headers = new Headers(suppliedHeaders);

    if (fetchOptions.body && !(fetchOptions.body instanceof FormData)) {
        headers.set("Content-Type", "application/json");
    }

    const response = await authenticatedFetch(`${BASE_URL}${path}`, {
        ...fetchOptions,
        headers,
        skipAuth,
    });

    if (response.status === 204) return null;

    const contentType = response.headers.get("content-type") ?? "";
    const body = contentType.includes("application/json")
        ? await response.json().catch(() => null)
        : await response.text().catch(() => "");

    if (!response.ok) {
        const validationErrors = body?.errors;
        const validationMessage = Array.isArray(validationErrors)
            ? validationErrors.map((error) => error.msg ?? error.message).filter(Boolean).join(" ")
            : validationErrors && typeof validationErrors === "object"
                ? Object.values(validationErrors).flat().filter(Boolean).join(" ")
                : "";
        const message = body?.message ?? body?.title ?? validationMessage ?? body;
        throw new Error(
            typeof message === "string" && message.length > 0
                ? message
                : response.statusText || "Request failed."
        );
    }

    return body;
}

export default request;
