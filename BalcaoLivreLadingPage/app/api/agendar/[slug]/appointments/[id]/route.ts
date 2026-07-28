import {
  agendaErrorResponse,
  getBookingStatus,
} from "../../../../../lib/agenda-booking-server";

type RouteContext = { params: Promise<{ slug: string; id: string }> };

export async function GET(request: Request, context: RouteContext) {
  try {
    const { slug, id } = await context.params;
    return await getBookingStatus(request, slug, id);
  } catch (error) {
    return agendaErrorResponse(error);
  }
}
