import {
  agendaErrorResponse,
  getAvailability,
} from "../../../../lib/agenda-booking-server";

type RouteContext = { params: Promise<{ slug: string }> };

export async function GET(_request: Request, context: RouteContext) {
  try {
    const { slug } = await context.params;
    return await getAvailability(slug);
  } catch (error) {
    return agendaErrorResponse(error);
  }
}
