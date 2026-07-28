import {
  agendaErrorResponse,
  getCatalogMedia,
} from "../../../../../lib/agenda-booking-server";

type RouteContext = {
  params: Promise<{ slug: string; mediaId: string }>;
};

export async function GET(_request: Request, context: RouteContext) {
  try {
    const { slug, mediaId } = await context.params;
    return await getCatalogMedia(slug, mediaId);
  } catch (error) {
    return agendaErrorResponse(error);
  }
}
