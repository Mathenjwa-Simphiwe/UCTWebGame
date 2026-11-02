import { request } from "./client";
import { setAuthToken } from "../middleware/authMiddleware";

/** Create a game account and retain the JWT returned by the API. */
export async function register({ username, email, password }) {
    const response = await request("/auth/register", {
        method: "POST",
        body: JSON.stringify({ username, email, password }),
        skipAuth: true,
    });

    const token = response?.accessToken ?? response?.token;
    if (typeof token !== "string" || token.length === 0) {
        throw new Error("The registration response did not include an access token.");
    }

    setAuthToken(token);
    return response.user;
}

export default register;
