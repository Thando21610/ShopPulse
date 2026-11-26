export async function onRequest(context) {
    const upstream = new URL("/health", context.env.API_ORIGIN);
    const upstreamRequest = new Request(upstream, {
        method: context.request.method,
        headers: context.request.headers,
        redirect: "manual"
    });

    return fetch(upstreamRequest);
}
