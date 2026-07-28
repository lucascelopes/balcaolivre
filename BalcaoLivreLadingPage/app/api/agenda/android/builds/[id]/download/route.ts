import {
  agendaAndroidErrorResponse,
  downloadAgendaAndroidBuild,
} from "../../../../../../lib/agenda-android-server";

type RouteContext = { params: Promise<{ id: string }> };

export async function GET(request: Request, context: RouteContext) {
  try {
    const { id } = await context.params;
    return await downloadAgendaAndroidBuild(request, id);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}
