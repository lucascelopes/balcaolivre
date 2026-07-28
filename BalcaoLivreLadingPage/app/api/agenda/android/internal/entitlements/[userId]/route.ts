import {
  agendaAndroidErrorResponse,
  agendaAndroidOptionsResponse,
  updateInternalAgendaAndroidEntitlement,
} from "../../../../../../lib/agenda-android-server";

type RouteContext = { params: Promise<{ userId: string }> };

export async function POST(request: Request, context: RouteContext) {
  try {
    const { userId } = await context.params;
    return await updateInternalAgendaAndroidEntitlement(request, userId);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAndroidOptionsResponse("POST, OPTIONS");
}
